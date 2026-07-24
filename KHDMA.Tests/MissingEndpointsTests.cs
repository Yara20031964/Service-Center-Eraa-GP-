using System.Text.Json;
using KHDMA.Application.DTOs.Provider;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.RealTime;
using KHDMA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Tests;

public class BookingDetailsServiceTests
{
    [Fact]
    public async Task PendingBooking_IsVisibleToCustomer_WithNullProviderFields()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);
        var bookingId = harness.SeedBooking(world);

        await using var db = harness.NewContext();
        var result = await new BookingDetailsService(db, new PassthroughImageUrlResolver())
            .GetAsync(bookingId, world.CustomerId);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Pipe Leakage Repair", result.Data!.ServiceName);
        Assert.Equal("Searching...", result.Data.StatusLabelEn);
        Assert.Null(result.Data.ProviderId);
        Assert.Null(result.Data.ProviderName);
        Assert.Null(result.Data.ProviderPhone);
        Assert.Null(result.Data.ProviderRating);
        Assert.Null(result.Data.ProviderPhoto);
    }

    [Fact]
    public async Task BookingDetail_IsForbiddenToANonParticipant()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);
        var bookingId = harness.SeedBooking(world);

        await using var db = harness.NewContext();
        var result = await new BookingDetailsService(db, new PassthroughImageUrlResolver())
            .GetAsync(bookingId, "not-a-participant");

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task AssignedProvider_CanReadBooking_WithProviderContact()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);
        var providerId = world.ProviderIds[0];
        var bookingId = harness.SeedBooking(world);

        await using (var setup = harness.NewContext())
        {
            await setup.Bookings
                .Where(b => b.Id == bookingId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.ProviderId, providerId)
                    .SetProperty(b => b.Status, BookingStatus.Accepted)
                    .SetProperty(b => b.AcceptedAt, DateTime.UtcNow));
        }

        await using var db = harness.NewContext();
        var result = await new BookingDetailsService(db, new PassthroughImageUrlResolver())
            .GetAsync(bookingId, providerId);

        Assert.True(result.Success);
        Assert.Equal(providerId, result.Data!.ProviderId);
        Assert.Equal("Provider 0", result.Data.ProviderName);
        Assert.Equal("+201000000000", result.Data.ProviderPhone);
        Assert.NotNull(result.Data.AcceptedAt);
    }
}

public class ProviderJobsServiceTests
{
    [Fact]
    public async Task PendingJobs_ReturnsOnlyCurrentCandidate_WithoutAddress()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 2);
        var bookingId = harness.SeedBooking(world);
        await harness.Candidates.SetAsync(
            bookingId, [world.ProviderIds[0]], TimeSpan.FromMinutes(2));

        await using var db = harness.NewContext();
        var service = new ProviderJobsService(
            db, harness.Candidates, new StubPricingService(212.50m),
            new PassthroughImageUrlResolver());

        var included = await service.GetPendingJobsAsync(world.ProviderIds[0]);
        var excluded = await service.GetPendingJobsAsync(world.ProviderIds[1]);

        var card = Assert.Single(included.Data!);
        Assert.Equal(bookingId, card.BookingId);
        Assert.Equal(212.50m, card.ProviderEarning);
        Assert.InRange(card.SecondsRemaining, 1, 60);
        Assert.Empty(excluded.Data!);

        var json = JsonSerializer.Serialize(card);
        Assert.DoesNotContain("Gardenia", json);
        Assert.DoesNotContain("\"address\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Availability_UpdatesCoordinatesOnlyWhenBothAreSupplied()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);
        var providerId = world.ProviderIds[0];

        await using var db = harness.NewContext();
        var service = new ProviderJobsService(
            db, harness.Candidates, new StubPricingService(212.50m),
            new PassthroughImageUrlResolver());

        var original = await db.Providers
            .AsNoTracking()
            .SingleAsync(p => p.ApplicationUserId == providerId);

        var partial = await service.UpdateAvailabilityAsync(providerId, new UpdateAvailabilityDto
        {
            Status = AvailabilityStatus.Offline,
            Latitude = 31.0,
        });

        Assert.Equal(AvailabilityStatus.Offline, partial.Data!.Status);
        Assert.Equal(original.CurrentLatitude, partial.Data.Latitude);
        Assert.Equal(original.CurrentLongitude, partial.Data.Longitude);

        var complete = await service.UpdateAvailabilityAsync(providerId, new UpdateAvailabilityDto
        {
            Status = AvailabilityStatus.Online,
            Latitude = 30.10,
            Longitude = 31.30,
        });

        Assert.Equal(AvailabilityStatus.Online, complete.Data!.Status);
        Assert.Equal(30.10, complete.Data.Latitude);
        Assert.Equal(31.30, complete.Data.Longitude);
    }

    [Fact]
    public async Task PendingJobs_WithoutProviderCoordinates_ReturnsSuccessfulEmptyList()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 1);
        var providerId = world.ProviderIds[0];

        await using (var setup = harness.NewContext())
        {
            await setup.Providers
                .Where(p => p.ApplicationUserId == providerId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.CurrentLatitude, (double?)null)
                    .SetProperty(p => p.CurrentLongitude, (double?)null));
        }

        await using var db = harness.NewContext();
        var service = new ProviderJobsService(
            db, harness.Candidates, new StubPricingService(212.50m),
            new PassthroughImageUrlResolver());

        var result = await service.GetPendingJobsAsync(providerId);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Empty(result.Data!);
    }
}

public class PublicProviderListingTests
{
    private static PublicCatalogService NewCatalog(TestHarness harness, Infrastructure.Data.AppDbContext db)
        => new(db, new StubPricingService(212.50m), new InMemoryPresenceStore(),
            new PassthroughImageUrlResolver());

    [Fact]
    public async Task Listing_FiltersByCategoryAndSearch_AndOrdersByRating()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 3);

        await using (var setup = harness.NewContext())
        {
            await setup.Providers
                .Where(p => p.ApplicationUserId == world.ProviderIds[1])
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Rating, 4.9));
            await setup.ProviderServices
                .Where(ps => ps.ProviderId == world.ProviderIds[2])
                .ExecuteUpdateAsync(s => s.SetProperty(ps => ps.IsActive, false));
        }

        await using var db = harness.NewContext();
        var categoryId = await db.Services
            .Where(s => s.id == world.ServiceId)
            .Select(s => s.CategoryId)
            .SingleAsync();

        var result = await NewCatalog(harness, db).GetProvidersAsync(
            categoryId, "Plumber", null, null, null, 1, 10);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(world.ProviderIds[1], result.Data.First().Id);
        Assert.DoesNotContain(result.Data, p => p.Id == world.ProviderIds[2]);
        Assert.All(result.Data, p => Assert.Null(p.DistanceKm));
    }

    [Fact]
    public async Task NearbyListing_FiltersRadiusAndOrdersByDistance()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 3);

        await using (var setup = harness.NewContext())
        {
            await setup.Providers
                .Where(p => p.ApplicationUserId == world.ProviderIds[2])
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.CurrentLatitude, 31.2001)
                    .SetProperty(p => p.CurrentLongitude, 29.9187));
            await setup.Providers
                .Where(p => p.ApplicationUserId == world.ProviderIds[1])
                .ExecuteUpdateAsync(s => s.SetProperty(
                    p => p.AvailabilityStatus, AvailabilityStatus.Offline));
        }

        await using var db = harness.NewContext();
        var catalog = NewCatalog(harness, db);
        var result = await catalog.GetProvidersAsync(
            null, null, 30.0444, 31.2357, 5, 1, 10);
        var cards = result.Data.ToList();

        var nearbyProvider = Assert.Single(cards);
        Assert.Equal(world.ProviderIds[0], nearbyProvider.Id);
        Assert.DoesNotContain(cards, p => p.Id == world.ProviderIds[2]);

        var browse = await catalog.GetProvidersAsync(
            null, null, null, null, null, 1, 10);

        Assert.Contains(browse.Data, p => p.Id == world.ProviderIds[1]);
    }

    [Fact]
    public async Task Listing_ExcludesInactiveAndDeletedProviders()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 3);

        await using (var setup = harness.NewContext())
        {
            await setup.Providers
                .Where(p => p.ApplicationUserId == world.ProviderIds[0])
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.State, ProviderState.Suspended));
            await setup.Users
                .Where(u => u.Id == world.ProviderIds[1])
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsDeleted, true));
        }

        await using var db = harness.NewContext();
        var result = await NewCatalog(harness, db).GetProvidersAsync(
            null, null, null, null, null, 1, 10);

        var provider = Assert.Single(result.Data);
        Assert.Equal(world.ProviderIds[2], provider.Id);
    }
}
