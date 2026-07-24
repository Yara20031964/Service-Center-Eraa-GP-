using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KHDMA.Tests;

/// <summary>
/// A throwaway SQLite database plus the fakes the dispatch path needs.
/// </summary>
/// <remarks>
/// SQLite in-memory rather than the EF in-memory provider, because the accept
/// race is decided by a conditional UPDATE and the in-memory provider does not
/// execute real SQL. The connection is held open for the fixture's lifetime -
/// closing it destroys the database.
///
/// Everything runs on the in-process stores, so CI needs no Redis.
/// </remarks>
public sealed class TestHarness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public RecordingBookingNotifier Notifier { get; } = new();
    public ILockService Locks { get; }
    public IDispatchCandidateStore Candidates { get; }

    public TestHarness()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = NewContext();
        db.Database.EnsureCreated();

        // Fresh instances per harness: the in-memory stores keep static state, so
        // sharing them across tests would leak locks between cases.
        Locks = new Infrastructure.RealTime.InMemoryLockService();
        Candidates = new Infrastructure.RealTime.InMemoryDispatchCandidateStore();
    }

    /// <summary>
    /// A new context over the same database. Each concurrent caller needs its own -
    /// DbContext is not thread-safe.
    /// </summary>
    public AppDbContext NewContext() => new(_options);

    public Infrastructure.Services.DispatchService NewDispatchService(AppDbContext db, decimal providerEarning = 212.50m)
        => new(
            db,
            Notifier,
            Locks,
            Candidates,
            new StubPricingService(providerEarning),
            new PassthroughImageUrlResolver(),
            Microsoft.Extensions.Options.Options.Create(new Application.Common.DispatchSettings()),
            NullLogger<Infrastructure.Services.DispatchService>.Instance);

    // ------------------------------------------------------------------
    // Seeding
    // ------------------------------------------------------------------

    public record SeededWorld(Guid ServiceId, string CustomerId, IReadOnlyList<string> ProviderIds);

    /// <summary>Creates a category, a service, one customer and <paramref name="providerCount"/> providers.</summary>
    public SeededWorld Seed(int providerCount = 3, double customerLat = 30.0444, double customerLng = 31.2357)
    {
        using var db = NewContext();

        var category = new Category { NameEn = "Plumbing", NameAr = "سباكة" };
        var service = new Service
        {
            CategoryId = category.id,
            NameEn = "Pipe Leakage Repair",
            NameAr = "إصلاح تسرب المواسير",
            FixedPrice = 250m,
            EstimatedDurationMin = 45,
            EstimatedDurationMax = 90,
            IsActive = true,
        };

        var customerUser = NewUser("customer@test.local", "Sara Ahmed", UserRole.Customer);
        var customer = new Customer { ApplicationUserId = customerUser.Id };

        db.Categories.Add(category);
        db.Services.Add(service);
        db.Users.Add(customerUser);
        db.Customers.Add(customer);

        var providerIds = new List<string>();

        for (var i = 0; i < providerCount; i++)
        {
            var user = NewUser($"provider{i}@test.local", $"Provider {i}", UserRole.Provider);
            db.Users.Add(user);

            db.Providers.Add(new Provider
            {
                ApplicationUserId = user.Id,
                State = ProviderState.Active,
                AvailabilityStatus = AvailabilityStatus.Online,
                // Spread them a few hundred metres apart so distance ordering is
                // deterministic but everyone stays inside the 10km first round.
                WorkingLatitude = customerLat + i * 0.005,
                WorkingLongitude = customerLng + i * 0.005,
                CurrentLatitude = customerLat + i * 0.005,
                CurrentLongitude = customerLng + i * 0.005,
                JobTitle = "Plumber",
                Rating = 4.5,
                ReviewCount = 10,
            });

            db.ProviderServices.Add(new ProviderService
            {
                ProviderId = user.Id,
                ServiceId = service.id,
                IsActive = true,
            });

            providerIds.Add(user.Id);
        }

        db.SaveChanges();
        return new SeededWorld(service.id, customerUser.Id, providerIds);
    }

    public Guid SeedBooking(
        SeededWorld world,
        BookingStatus status = BookingStatus.Dispatching,
        double lat = 30.0444,
        double lng = 31.2357)
    {
        using var db = NewContext();

        var booking = new Booking
        {
            CustomerId = world.CustomerId,
            ProviderId = null,
            ServiceId = world.ServiceId,
            BookingType = BookingType.Immediate,
            Status = status,
            Latitude = lat,
            Longitude = lng,
            Address = "123 Gardenia St, New Cairo",
            TotalPrice = 275m,
            DispatchDeadline = status == BookingStatus.Dispatching ? DateTime.UtcNow.AddSeconds(60) : null,
            DispatchRoundCount = status == BookingStatus.Dispatching ? 1 : 0,
        };

        db.Bookings.Add(booking);
        db.Payments.Add(new Payment
        {
            BookingId = booking.Id,
            Amount = 275m,
            ServiceFee = 250m,
            VatAmount = 25m,
            CommissionAmount = 37.50m,
            ProviderEarning = 212.50m,
            PaymentStatus = PaymentStatus.Paid,
            PaidAt = DateTime.UtcNow,
        });

        db.SaveChanges();
        return booking.Id;
    }

    private static ApplicationUser NewUser(string email, string fullName, UserRole role) => new()
    {
        Id = Guid.NewGuid().ToString(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        FullName = fullName,
        Role = role,
        Status = UserStatus.Active,
        PhoneNumber = "+201000000000",
        SecurityStamp = Guid.NewGuid().ToString(),
    };

    public void Dispose() => _connection.Dispose();
}

/// <summary>Captures every notification so tests can assert on what was pushed.</summary>
public class RecordingBookingNotifier : IBookingNotifier
{
    private readonly object _gate = new();

    public List<(Guid BookingId, BookingStatus Status)> StatusChanges { get; } = [];
    public List<(string ProviderId, JobCardDto Card)> JobsDispatched { get; } = [];
    public List<(Guid BookingId, ProviderCardDto Provider)> ProvidersAssigned { get; } = [];
    public List<(IEnumerable<string> ProviderIds, Guid BookingId)> JobsTaken { get; } = [];
    public List<(IEnumerable<string> ProviderIds, Guid BookingId)> JobsExpired { get; } = [];
    public List<(Guid BookingId, int Rounds, bool Refunded)> NoProviderEvents { get; } = [];

    public Task BookingStatusChangedAsync(Guid bookingId, BookingStatus status, string? eta = null, string? message = null)
    {
        lock (_gate) StatusChanges.Add((bookingId, status));
        return Task.CompletedTask;
    }

    public Task ProviderAssignedAsync(Guid bookingId, ProviderCardDto provider)
    {
        lock (_gate) ProvidersAssigned.Add((bookingId, provider));
        return Task.CompletedTask;
    }

    public Task ProviderLocationAsync(Guid bookingId, ProviderLocationDto location) => Task.CompletedTask;

    public Task NoProviderFoundAsync(Guid bookingId, int roundsTried, double lastRadiusKm, bool refunded)
    {
        lock (_gate) NoProviderEvents.Add((bookingId, roundsTried, refunded));
        return Task.CompletedTask;
    }

    public Task PaymentStatusChangedAsync(Guid bookingId, PaymentStatusEventDto payment) => Task.CompletedTask;

    public Task JobDispatchedAsync(string providerId, JobCardDto card)
    {
        lock (_gate) JobsDispatched.Add((providerId, card));
        return Task.CompletedTask;
    }

    public Task JobDispatchExpiredAsync(IEnumerable<string> providerIds, Guid bookingId)
    {
        lock (_gate) JobsExpired.Add((providerIds.ToList(), bookingId));
        return Task.CompletedTask;
    }

    public Task JobTakenAsync(IEnumerable<string> providerIds, Guid bookingId)
    {
        lock (_gate) JobsTaken.Add((providerIds.ToList(), bookingId));
        return Task.CompletedTask;
    }

    public Task JobCancelledAsync(string providerId, Guid bookingId, string? reason) => Task.CompletedTask;
}

/// <summary>Fixed 250 EGP / 10% VAT / 15% commission, so tests assert on known numbers.</summary>
public class StubPricingService : IPricingService
{
    private readonly decimal _providerEarning;

    public StubPricingService(decimal providerEarning) => _providerEarning = providerEarning;

    public Task<PriceBreakdown?> ForServiceAsync(Guid serviceId)
        => Task.FromResult<PriceBreakdown?>(Build(250m));

    public Task<PriceBreakdown> ForServiceFeeAsync(decimal serviceFee)
        => Task.FromResult(Build(serviceFee));

    private PriceBreakdown Build(decimal serviceFee)
        => new(serviceFee, 0.10m, serviceFee * 0.10m, serviceFee * 1.10m,
               0.15m, serviceFee * 0.15m, _providerEarning, "EGP");
}

public sealed class PassthroughImageUrlResolver : IImageUrlResolver
{
    public string? Resolve(string? path) => path;
}
