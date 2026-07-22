using KHDMA.Application.Interfaces;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KHDMA.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Drives bookings the customer scheduled for later.
    /// </summary>
    /// <remarks>
    /// Two things happen on the run-up to a scheduled slot:
    /// <list type="bullet">
    /// <item>T-60m: remind the customer.</item>
    /// <item>T-30m: start dispatching, so the provider has travel time.</item>
    /// </list>
    /// Both are idempotent - the reminder is stamped on <c>ProviderNotifiedAt</c>
    /// and dispatch flips the status - so a restart cannot double-send or
    /// double-dispatch.
    /// </remarks>
    public class ScheduledBookingWorker : BackgroundService
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan DispatchLeadTime = TimeSpan.FromMinutes(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledBookingWorker> _logger;

        public ScheduledBookingWorker(IServiceScopeFactory scopeFactory, ILogger<ScheduledBookingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ScheduledBookingWorker started ({Interval}s tick)", TickInterval.TotalSeconds);

            using var timer = new PeriodicTimer(TickInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var dispatch = scope.ServiceProvider.GetRequiredService<IDispatchService>();

                    await SendRemindersAsync(db, stoppingToken);
                    await DispatchDueBookingsAsync(db, dispatch, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ScheduledBookingWorker tick failed; continuing");
                }
            }

            _logger.LogInformation("ScheduledBookingWorker stopped");
        }

        private async Task SendRemindersAsync(AppDbContext db, CancellationToken ct)
        {
            var threshold = DateTime.UtcNow.Add(ReminderLeadTime);

            var due = await db.Bookings
                .Where(b => b.BookingType == BookingType.Scheduled
                         && b.Status == BookingStatus.Pending
                         && b.ProviderNotifiedAt == null
                         && b.ScheduledTime != null
                         && b.ScheduledTime <= threshold)
                .Select(b => new { b.Id, b.CustomerId, b.ScheduledTime, ServiceName = b.Service.NameEn })
                .Take(100)
                .ToListAsync(ct);

            if (due.Count == 0) return;

            foreach (var b in due)
            {
                db.Notifications.Add(new Notification
                {
                    UserId = b.CustomerId,
                    BookingId = b.Id,
                    Type = "BookingReminder",
                    Title = "Your booking is coming up",
                    Body = $"Your {b.ServiceName} booking is scheduled for {b.ScheduledTime:HH:mm}. " +
                           "We will start finding a provider shortly.",
                });
            }

            // Stamped in the same transaction as the notifications, so a crash
            // between the two cannot resend them.
            var ids = due.Select(x => x.Id).ToList();
            await db.Bookings
                .Where(b => ids.Contains(b.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.ProviderNotifiedAt, DateTime.UtcNow), ct);

            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Sent {Count} scheduled-booking reminder(s)", due.Count);
        }

        private async Task DispatchDueBookingsAsync(AppDbContext db, IDispatchService dispatch, CancellationToken ct)
        {
            var threshold = DateTime.UtcNow.Add(DispatchLeadTime);

            var due = await db.Bookings
                .Where(b => b.BookingType == BookingType.Scheduled
                         && b.Status == BookingStatus.Pending
                         && b.ProviderId == null
                         && b.ScheduledTime != null
                         && b.ScheduledTime <= threshold)
                .Select(b => b.Id)
                .Take(50)
                .ToListAsync(ct);

            foreach (var bookingId in due)
            {
                try
                {
                    var result = await dispatch.DispatchAsync(bookingId, ct);
                    _logger.LogInformation(
                        "Scheduled booking {BookingId} dispatched: {Outcome} ({Count} providers)",
                        bookingId, result.Outcome, result.ProvidersNotified);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to dispatch scheduled booking {BookingId}", bookingId);
                }
            }
        }
    }
}
