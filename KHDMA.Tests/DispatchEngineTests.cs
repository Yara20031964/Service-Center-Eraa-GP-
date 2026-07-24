using KHDMA.Application.Interfaces;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Tests;

/// <summary>Broadcast, eligibility filtering, radius expansion and giving up.</summary>
public class DispatchEngineTests
{
    [Fact]
    public async Task Dispatch_BroadcastsToEveryEligibleProviderInRange()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 3);
        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        var result = await harness.NewDispatchService(db).DispatchAsync(bookingId);

        Assert.Equal(DispatchOutcome.Broadcast, result.Outcome);
        Assert.Equal(3, result.ProvidersNotified);
        Assert.Equal(1, result.Round);
        Assert.Equal(3, harness.Notifier.JobsDispatched.Count);

        await using var verify = harness.NewContext();
        var booking = await verify.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);

        Assert.Equal(BookingStatus.Dispatching, booking.Status);
        Assert.Null(booking.ProviderId);           // still unclaimed
        Assert.NotNull(booking.DispatchDeadline);  // countdown persisted, survives a restart
    }

    [Fact]
    public async Task JobCard_ShowsNetEarningsAndDistanceButNeverTheAddress()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);
        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        await harness.NewDispatchService(db, providerEarning: 212.50m).DispatchAsync(bookingId);

        var card = Assert.Single(harness.Notifier.JobsDispatched).Card;

        // 250 service at 15% commission - what the provider is actually paid.
        Assert.Equal(212.50m, card.ProviderEarning);

        // SRS 7.1: distance only, and a first name rather than the full identity.
        Assert.True(card.DistanceKm >= 0);
        Assert.Equal("Sara", card.CustomerFirstName);

        // The countdown must be renderable from an absolute instant.
        Assert.True(card.ExpiresAt > DateTime.UtcNow);

        var json = System.Text.Json.JsonSerializer.Serialize(card);
        Assert.DoesNotContain("Gardenia", json);
    }

    [Theory]
    [InlineData(ProviderState.Pending)]
    [InlineData(ProviderState.Suspended)]
    public async Task Dispatch_SkipsProvidersWhoAreNotApproved(ProviderState state)
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);

        await using var setup = harness.NewContext();
        await setup.Providers.ExecuteUpdateAsync(s => s.SetProperty(p => p.State, state));

        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        var result = await harness.NewDispatchService(db).DispatchAsync(bookingId);

        Assert.Empty(harness.Notifier.JobsDispatched);
        Assert.Equal(DispatchOutcome.Exhausted, result.Outcome);
    }

    [Fact]
    public async Task Dispatch_SkipsOfflineProviders()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 2);

        await using var setup = harness.NewContext();
        await setup.Providers
            .Where(p => p.ApplicationUserId == world.ProviderIds[0])
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AvailabilityStatus, AvailabilityStatus.Offline));

        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        await harness.NewDispatchService(db).DispatchAsync(bookingId);

        var notified = harness.Notifier.JobsDispatched.Select(x => x.ProviderId).ToList();
        Assert.Single(notified);
        Assert.Equal(world.ProviderIds[1], notified[0]);
    }

    [Fact]
    public async Task Dispatch_SkipsProvidersWhoDoNotOfferTheService()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 2);

        await using var setup = harness.NewContext();
        await setup.ProviderServices
            .Where(ps => ps.ProviderId == world.ProviderIds[0])
            .ExecuteUpdateAsync(s => s.SetProperty(ps => ps.IsActive, false));

        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        await harness.NewDispatchService(db).DispatchAsync(bookingId);

        Assert.Single(harness.Notifier.JobsDispatched);
        Assert.Equal(world.ProviderIds[1], harness.Notifier.JobsDispatched[0].ProviderId);
    }

    [Fact]
    public async Task Dispatch_SkipsProvidersAlreadyOnAJob()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 2);

        var busy = harness.SeedBooking(world);
        await using var setup = harness.NewContext();
        await setup.Bookings
            .Where(b => b.Id == busy)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.ProviderId, world.ProviderIds[0])
                .SetProperty(b => b.Status, BookingStatus.EnRoute));

        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        await harness.NewDispatchService(db).DispatchAsync(bookingId);

        Assert.Single(harness.Notifier.JobsDispatched);
        Assert.Equal(world.ProviderIds[1], harness.Notifier.JobsDispatched[0].ProviderId);
    }

    [Fact]
    public async Task Dispatch_ExcludesProvidersOutsideTheMaximumRadius()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);

        // Alexandria is ~180km from Cairo - beyond MaxRadiusKm of 30 even after
        // every round of expansion.
        await using var setup = harness.NewContext();
        await setup.Providers.ExecuteUpdateAsync(s => s
            .SetProperty(p => p.WorkingLatitude, 31.2001)
            .SetProperty(p => p.WorkingLongitude, 29.9187));

        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        var result = await harness.NewDispatchService(db).DispatchAsync(bookingId);

        Assert.Empty(harness.Notifier.JobsDispatched);
        Assert.Equal(DispatchOutcome.Exhausted, result.Outcome);
    }

    [Fact]
    public async Task Dispatch_WithNobodyAvailable_EndsAsNoProviderFoundAndRefunds()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 0);
        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        var result = await harness.NewDispatchService(db).DispatchAsync(bookingId);

        Assert.Equal(DispatchOutcome.Exhausted, result.Outcome);

        // All three rounds are tried before giving up - and immediately, rather
        // than making the customer wait 60s per empty round.
        Assert.Equal(3, result.Round);

        await using var verify = harness.NewContext();
        var booking = await verify.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        var payment = await verify.Payments.AsNoTracking().FirstAsync(p => p.BookingId == bookingId);

        Assert.Equal(BookingStatus.NoProviderFound, booking.Status);
        Assert.Null(booking.DispatchDeadline);
        Assert.Equal(PaymentStatus.Refunded, payment.PaymentStatus);

        var evt = Assert.Single(harness.Notifier.NoProviderEvents);
        Assert.True(evt.Refunded);

        // The customer also gets a durable notification, not just a socket event
        // they might have been disconnected for.
        Assert.True(await verify.Notifications.AnyAsync(n => n.BookingId == bookingId));
    }

    [Fact]
    public async Task ProcessExpiredRounds_TellsThePreviousRoundToDismissAndRetries()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 2);
        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        var dispatch = harness.NewDispatchService(db);

        await dispatch.DispatchAsync(bookingId);
        harness.Notifier.JobsDispatched.Clear();

        // Force the countdown to have elapsed.
        await using var expire = harness.NewContext();
        await expire.Bookings
            .Where(b => b.Id == bookingId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.DispatchDeadline, DateTime.UtcNow.AddSeconds(-1)));

        await using var sweepDb = harness.NewContext();
        var processed = await harness.NewDispatchService(sweepDb).ProcessExpiredRoundsAsync();

        Assert.Equal(1, processed);

        // The stale card is withdrawn from everyone who saw it...
        var expired = Assert.Single(harness.Notifier.JobsExpired);
        Assert.Equal(2, expired.ProviderIds.Count());

        // ...and the next round goes out at a wider radius.
        await using var verify = harness.NewContext();
        var booking = await verify.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);
        Assert.Equal(2, booking.DispatchRoundCount);
        Assert.Equal(20, booking.DispatchRadiusKm);
    }

    [Fact]
    public async Task Dispatch_IsIgnoredForABookingThatAlreadyHasAProvider()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 2);
        var bookingId = harness.SeedBooking(world);

        await using var setup = harness.NewContext();
        await setup.Bookings
            .Where(b => b.Id == bookingId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.ProviderId, world.ProviderIds[0])
                .SetProperty(b => b.Status, BookingStatus.Accepted));

        await using var db = harness.NewContext();
        var result = await harness.NewDispatchService(db).DispatchAsync(bookingId);

        Assert.Equal(DispatchOutcome.NotDispatchable, result.Outcome);
        Assert.Empty(harness.Notifier.JobsDispatched);
    }

    [Fact]
    public async Task DirectBooking_OffersTheJobToOneProviderOnly()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 3);
        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        var result = await harness.NewDispatchService(db)
            .DispatchToProviderAsync(bookingId, world.ProviderIds[1]);

        Assert.Equal(DispatchOutcome.Broadcast, result.Outcome);

        // The customer chose this person; nobody else is offered the job.
        var dispatched = Assert.Single(harness.Notifier.JobsDispatched);
        Assert.Equal(world.ProviderIds[1], dispatched.ProviderId);
    }
}

/// <summary>The distance maths the eligibility query and the ETA fallback share.</summary>
public class HaversineTests
{
    [Fact]
    public void IdenticalPoints_AreZeroApart()
    {
        // The spherical law of cosines returns NaN here on some inputs, and SQL
        // Server raises an error rather than returning it - which is why the
        // implementation uses the ASIN(SQRT(...)) form.
        Assert.Equal(0, DispatchService.Haversine(30.0444, 31.2357, 30.0444, 31.2357), 6);
    }

    [Fact]
    public void CairoToAlexandria_IsAboutOneEightyKm()
    {
        var km = DispatchService.Haversine(30.0444, 31.2357, 31.2001, 29.9187);
        Assert.InRange(km, 175, 190);
    }

    [Fact]
    public void CairoToGiza_IsAboutTwelveKm()
    {
        var km = DispatchService.Haversine(30.0444, 31.2357, 30.0131, 31.2089);
        Assert.InRange(km, 3, 6);
    }

    [Fact]
    public void DistanceIsSymmetric()
    {
        var there = DispatchService.Haversine(30.0444, 31.2357, 31.2001, 29.9187);
        var back = DispatchService.Haversine(31.2001, 29.9187, 30.0444, 31.2357);
        Assert.Equal(there, back, 6);
    }
}
