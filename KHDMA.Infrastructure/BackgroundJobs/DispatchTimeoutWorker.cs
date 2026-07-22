using KHDMA.Application.Common;
using KHDMA.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KHDMA.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Expires dispatch rounds nobody accepted, then re-dispatches at a wider radius.
    /// </summary>
    /// <remarks>
    /// The countdown lives in <c>Booking.DispatchDeadline</c> rather than in a
    /// timer, so a restart mid-round resumes correctly instead of leaving the
    /// booking stuck in Dispatching forever.
    /// </remarks>
    public class DispatchTimeoutWorker : BackgroundService
    {
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DispatchTimeoutWorker> _logger;

        public DispatchTimeoutWorker(IServiceScopeFactory scopeFactory, ILogger<DispatchTimeoutWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DispatchTimeoutWorker started ({Interval}s tick)", TickInterval.TotalSeconds);

            using var timer = new PeriodicTimer(TickInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);

                    // A fresh scope per tick: DbContext and the dispatch service are
                    // scoped, and holding one across the worker's lifetime would
                    // accumulate tracked entities for as long as the app runs.
                    using var scope = _scopeFactory.CreateScope();
                    var dispatch = scope.ServiceProvider.GetRequiredService<IDispatchService>();

                    var processed = await dispatch.ProcessExpiredRoundsAsync(stoppingToken);
                    if (processed > 0)
                        _logger.LogInformation("Processed {Count} expired dispatch round(s)", processed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;   // normal shutdown
                }
                catch (Exception ex)
                {
                    // Never let one bad tick kill the worker - a dead worker means
                    // every future booking hangs in Dispatching with no diagnosis.
                    _logger.LogError(ex, "DispatchTimeoutWorker tick failed; continuing");
                }
            }

            _logger.LogInformation("DispatchTimeoutWorker stopped");
        }
    }
}
