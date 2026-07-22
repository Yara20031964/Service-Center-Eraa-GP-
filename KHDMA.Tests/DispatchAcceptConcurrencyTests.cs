using KHDMA.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Tests;

/// <summary>
/// The first-accept race. This is the single most important guarantee in the
/// dispatch engine: a booking is broadcast to many providers at once, and
/// exactly one may end up assigned.
/// </summary>
public class DispatchAcceptConcurrencyTests
{
    [Fact]
    public async Task TenProvidersAcceptingAtOnce_ExactlyOneWins()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 10);
        var bookingId = harness.SeedBooking(world);

        await harness.Candidates.SetAsync(bookingId, world.ProviderIds.ToList(), TimeSpan.FromMinutes(1));

        // Each caller gets its own DbContext and its own service instance, the way
        // ten concurrent HTTP requests would.
        var contexts = world.ProviderIds.Select(_ => harness.NewContext()).ToList();

        try
        {
            var barrier = new TaskCompletionSource();

            var attempts = world.ProviderIds.Select((providerId, i) => Task.Run(async () =>
            {
                var dispatch = harness.NewDispatchService(contexts[i]);
                await barrier.Task;          // release them all together
                return await dispatch.AcceptAsync(bookingId, providerId);
            })).ToList();

            barrier.SetResult();
            var results = await Task.WhenAll(attempts);

            var winners = results.Where(r => r.Success).ToList();
            var losers = results.Where(r => !r.Success).ToList();

            Assert.Single(winners);
            Assert.Equal(9, losers.Count);

            // Losers must be told they lost the race, not that something broke -
            // the client dismisses the card silently on 409.
            Assert.All(losers, r => Assert.Equal(409, r.StatusCode));

            // And the database agrees with exactly one of them.
            await using var verify = harness.NewContext();
            var booking = await verify.Bookings.AsNoTracking().FirstAsync(b => b.Id == bookingId);

            Assert.Equal(BookingStatus.Accepted, booking.Status);
            Assert.NotNull(booking.ProviderId);
            Assert.Equal(winners[0].Data!.BookingId, booking.Id);
            Assert.Contains(booking.ProviderId, world.ProviderIds);
            Assert.NotNull(booking.AcceptedAt);
        }
        finally
        {
            foreach (var c in contexts) await c.DisposeAsync();
        }
    }

    [Fact]
    public async Task Accept_RevealsTheAddressOnlyToTheWinner()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 2);
        var bookingId = harness.SeedBooking(world);

        await using var db = harness.NewContext();
        var dispatch = harness.NewDispatchService(db);

        var winner = await dispatch.AcceptAsync(bookingId, world.ProviderIds[0]);
        var loser = await dispatch.AcceptAsync(bookingId, world.ProviderIds[1]);

        // SRS 7.1: the job card carried distance only; the street address appears
        // only once a provider has committed.
        Assert.True(winner.Success);
        Assert.Equal("123 Gardenia St, New Cairo", winner.Data!.Address);
        Assert.NotNull(winner.Data.CustomerPhone);

        Assert.False(loser.Success);
        Assert.Null(loser.Data);
    }

    [Fact]
    public async Task Accept_NotifiesTheCustomerAndDismissesTheLosers()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 3);
        var bookingId = harness.SeedBooking(world);
        await harness.Candidates.SetAsync(bookingId, world.ProviderIds.ToList(), TimeSpan.FromMinutes(1));

        await using var db = harness.NewContext();
        var dispatch = harness.NewDispatchService(db);

        var winnerId = world.ProviderIds[0];
        await dispatch.AcceptAsync(bookingId, winnerId);

        Assert.Contains(harness.Notifier.ProvidersAssigned, x => x.BookingId == bookingId);
        Assert.Contains(harness.Notifier.StatusChanges, x => x.Status == BookingStatus.Accepted);

        var taken = Assert.Single(harness.Notifier.JobsTaken);
        var told = taken.ProviderIds.ToList();

        // The two who lost are told; the winner is not.
        Assert.Equal(2, told.Count);
        Assert.DoesNotContain(winnerId, told);
    }

    [Fact]
    public async Task Accept_IsRejectedWhenTheBookingIsNotDispatching()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);

        // Pending means dispatch has not started - there is nothing to claim.
        var bookingId = harness.SeedBooking(world, BookingStatus.Pending);

        await using var db = harness.NewContext();
        var dispatch = harness.NewDispatchService(db);

        var result = await dispatch.AcceptAsync(bookingId, world.ProviderIds[0]);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Accept_IsRejectedWhenTheProviderAlreadyHasAJobInProgress()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);
        var providerId = world.ProviderIds[0];

        var busyBooking = harness.SeedBooking(world);
        var newBooking = harness.SeedBooking(world);

        await using var setup = harness.NewContext();
        await setup.Bookings
            .Where(b => b.Id == busyBooking)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.ProviderId, providerId)
                .SetProperty(b => b.Status, BookingStatus.InProgress));

        await using var db = harness.NewContext();
        var dispatch = harness.NewDispatchService(db);

        var result = await dispatch.AcceptAsync(newBooking, providerId);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("in progress", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
