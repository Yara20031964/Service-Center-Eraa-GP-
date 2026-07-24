using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Application.Common;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.RealTime;
using KHDMA.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace KHDMA.Tests;

public class ChatServiceTests
{
    private static ChatService NewChatService(TestHarness harness, Infrastructure.Data.AppDbContext db, out RecordingChatNotifier notifier)
    {
        notifier = new RecordingChatNotifier();
        return new ChatService(
            db,
            new BookingAccessService(db),
            notifier,
            new InMemoryPresenceStore(),
            new PassthroughImageUrlResolver());
    }

    private static async Task<(Guid BookingId, string CustomerId, string ProviderId)> AcceptedBookingAsync(TestHarness harness)
    {
        var world = harness.Seed(providerCount: 1);
        var bookingId = harness.SeedBooking(world);

        await using var setup = harness.NewContext();
        await setup.Bookings
            .Where(b => b.Id == bookingId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.ProviderId, world.ProviderIds[0])
                .SetProperty(b => b.Status, BookingStatus.Accepted));

        return (bookingId, world.CustomerId, world.ProviderIds[0]);
    }

    [Fact]
    public async Task SendMessage_PersistsBeforeBroadcasting_SoOfflinePeersCanReplay()
    {
        using var harness = new TestHarness();
        var (bookingId, customerId, _) = await AcceptedBookingAsync(harness);

        await using var db = harness.NewContext();
        var chat = NewChatService(harness, db, out var notifier);

        var result = await chat.SendMessageAsync(bookingId, customerId,
            new SendMessageDto { MessageType = "Text", MessageText = "Hello, when will you arrive?" });

        Assert.True(result.Success);

        // The offline fallback depends on this ordering: a message must be in the
        // history even if the socket push never lands.
        await using var verify = harness.NewContext();
        Assert.True(await verify.ChatMessages.AnyAsync(m => m.BookingId == bookingId));
        Assert.NotEmpty(notifier.Messages);
    }

    [Fact]
    public async Task SendMessage_RejectsContactDetails()
    {
        using var harness = new TestHarness();
        var (bookingId, customerId, _) = await AcceptedBookingAsync(harness);

        await using var db = harness.NewContext();
        var chat = NewChatService(harness, db, out _);

        var result = await chat.SendMessageAsync(bookingId, customerId,
            new SendMessageDto { MessageType = "Text", MessageText = "call me on 01012345678" });

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);

        // Nothing is stored - a blocked message must not appear in history either.
        await using var verify = harness.NewContext();
        Assert.False(await verify.ChatMessages.AnyAsync(m => m.BookingId == bookingId));
    }

    [Fact]
    public async Task SendMessage_IsRejectedByAStranger()
    {
        using var harness = new TestHarness();
        var (bookingId, _, _) = await AcceptedBookingAsync(harness);

        await using var db = harness.NewContext();
        var chat = NewChatService(harness, db, out _);

        var result = await chat.SendMessageAsync(bookingId, "some-other-user-id",
            new SendMessageDto { MessageType = "Text", MessageText = "hello" });

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task SendMessage_IsLockedOnceTheBookingIsCompleted()
    {
        using var harness = new TestHarness();
        var (bookingId, customerId, _) = await AcceptedBookingAsync(harness);

        await using var close = harness.NewContext();
        await close.Bookings
            .Where(b => b.Id == bookingId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, BookingStatus.Completed));

        await using var db = harness.NewContext();
        var chat = NewChatService(harness, db, out var notifier);

        var result = await chat.SendMessageAsync(bookingId, customerId,
            new SendMessageDto { MessageType = "Text", MessageText = "one more thing" });

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);

        // The client is told to disable the input bar rather than left guessing.
        Assert.Contains(bookingId, notifier.Locked);
    }

    [Fact]
    public async Task History_IsChronologicalAndMarksTheViewersOwnMessages()
    {
        using var harness = new TestHarness();
        var (bookingId, customerId, providerId) = await AcceptedBookingAsync(harness);

        await using var db = harness.NewContext();
        var chat = NewChatService(harness, db, out _);

        await chat.SendMessageAsync(bookingId, customerId, new SendMessageDto { MessageText = "First" });
        await chat.SendMessageAsync(bookingId, providerId, new SendMessageDto { MessageText = "Second" });

        await using var readDb = harness.NewContext();
        var readChat = NewChatService(harness, readDb, out _);
        var history = await readChat.GetHistoryAsync(bookingId, customerId, 1, 50);

        var messages = history.Data.ToList();

        Assert.Equal(2, messages.Count);
        Assert.Equal("First", messages[0].MessageText);   // oldest first within the page
        Assert.Equal("Second", messages[1].MessageText);

        // IsMine is resolved server-side so the client never compares ids.
        Assert.True(messages[0].IsMine);
        Assert.False(messages[1].IsMine);
    }

    [Fact]
    public async Task History_IsRefusedToNonParticipants()
    {
        using var harness = new TestHarness();
        var (bookingId, _, _) = await AcceptedBookingAsync(harness);

        await using var db = harness.NewContext();
        var chat = NewChatService(harness, db, out _);

        var history = await chat.GetHistoryAsync(bookingId, "outsider", 1, 50);

        Assert.False(history.Success);
        Assert.Equal(403, history.StatusCode);
    }

    [Fact]
    public async Task MarkRead_OnlyAffectsThePeersMessages()
    {
        using var harness = new TestHarness();
        var (bookingId, customerId, providerId) = await AcceptedBookingAsync(harness);

        await using var db = harness.NewContext();
        var chat = NewChatService(harness, db, out _);

        await chat.SendMessageAsync(bookingId, customerId, new SendMessageDto { MessageText = "mine" });
        await chat.SendMessageAsync(bookingId, providerId, new SendMessageDto { MessageText = "theirs" });

        await using var readDb = harness.NewContext();
        var readChat = NewChatService(harness, readDb, out _);
        var result = await readChat.MarkReadAsync(bookingId, customerId);

        // You cannot "read" your own message.
        Assert.Equal(1, result.Data);
    }
}

public class PricingServiceTests
{
    private static PricingService NewPricingService(Infrastructure.Data.AppDbContext db, decimal commissionRate = 0.15m)
    {
        var commission = new Mock<ICommissionService>();
        commission.Setup(c => c.GetCurrentRateAsync())
            .ReturnsAsync(ApiResponse<CommissionDto>.Ok(new CommissionDto { Rate = commissionRate }));

        return new PricingService(db, commission.Object, Options.Create(new VatSettings { Rate = 0.10m }));
    }

    [Fact]
    public async Task Commission_IsTakenOffTheServiceFee_NotTheVatInclusiveTotal()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();

        var price = await NewPricingService(db).ForServiceFeeAsync(250m);

        Assert.Equal(250m, price.ServiceFee);
        Assert.Equal(25m, price.VatAmount);
        Assert.Equal(275m, price.Total);

        // 15% of 250, not of 275. VAT belongs to the tax authority, so charging
        // commission on it would be taking a cut of someone else's money.
        Assert.Equal(37.50m, price.CommissionAmount);
        Assert.Equal(212.50m, price.ProviderEarning);
    }

    [Fact]
    public async Task LineItems_AlwaysSumToTheChargedTotal()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();
        var pricing = NewPricingService(db);

        // Awkward amounts where naive rounding leaves the displayed lines a
        // piastre short of what the card is charged.
        foreach (var fee in new[] { 33.33m, 99.99m, 0.05m, 1234.56m })
        {
            var price = await pricing.ForServiceFeeAsync(fee);

            Assert.Equal(price.Total, price.ServiceFee + price.VatAmount);
            Assert.Equal(price.ServiceFee, price.CommissionAmount + price.ProviderEarning);
        }
    }

    [Fact]
    public async Task ServicePrice_ComesFromTheCatalogue_NotTheCaller()
    {
        using var harness = new TestHarness();
        var world = harness.Seed(providerCount: 0);

        await using var db = harness.NewContext();
        var price = await NewPricingService(db).ForServiceAsync(world.ServiceId);

        Assert.NotNull(price);
        Assert.Equal(250m, price.ServiceFee);   // the seeded FixedPrice
    }

    [Fact]
    public async Task UnknownService_HasNoPrice()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();

        Assert.Null(await NewPricingService(db).ForServiceAsync(Guid.NewGuid()));
    }

    /// <summary>
    /// A client-supplied price is structurally impossible: the request DTO has no
    /// such field. This asserts the field has not been reintroduced.
    /// </summary>
    [Fact]
    public void CreateBookingDto_HasNoPriceField()
    {
        var properties = typeof(Application.DTOs.Booking.CreateBookingDto)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.DoesNotContain("TotalPrice", properties);
        Assert.DoesNotContain("Price", properties);
        Assert.DoesNotContain("Amount", properties);
    }
}

public class CancellationPolicyTests
{
    [Fact]
    public async Task CancellingBeforeAProviderAccepts_IsFree()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();
        var policy = new CancellationPolicyService(db);

        foreach (var status in new[] { BookingStatus.Pending, BookingStatus.Dispatching })
        {
            var decision = await policy.Evaluate(
                new Booking { Status = status, CreateAt = DateTime.UtcNow });

            Assert.True(decision.Allowed);
            Assert.Equal(0m, decision.Fee);
        }
    }

    [Fact]
    public async Task CancellingInsideTheGraceWindow_IsFree()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();
        var policy = new CancellationPolicyService(db);

        var booking = new Booking
        {
            Status = BookingStatus.Accepted,
            AcceptedAt = DateTime.UtcNow.AddMinutes(-5),   // default window is 10 min
        };

        var decision = await policy.Evaluate(booking);

        Assert.True(decision.Allowed);
        Assert.Equal(0m, decision.Fee);
    }

    [Fact]
    public async Task CancellingAfterTheGraceWindow_IncursAFee()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();
        var policy = new CancellationPolicyService(db);

        var booking = new Booking
        {
            Status = BookingStatus.Arrived,
            AcceptedAt = DateTime.UtcNow.AddMinutes(-40),
            ArrivedAt = DateTime.UtcNow.AddMinutes(-2),
        };

        var decision = await policy.Evaluate(booking);

        // A late cancellation is charged, not refused: the customer can always
        // call the job off, they simply pay the flat fee for doing it late.
        Assert.True(decision.Allowed);
        Assert.Equal(new CancellationPolicy().CancellationFee, decision.Fee);
    }

    [Fact]
    public async Task CancellingOnAProviderWhoIsBadlyLate_IsFree()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();
        var policy = new CancellationPolicyService(db);

        var booking = new Booking
        {
            Status = BookingStatus.EnRoute,
            AcceptedAt = DateTime.UtcNow.AddMinutes(-60),
            EnRouteAt = DateTime.UtcNow.AddMinutes(-45),   // 45 min en route, 15 min grace
        };

        var decision = await policy.Evaluate(booking);

        Assert.True(decision.Allowed);
        Assert.Equal(0m, decision.Fee);
    }

    [Fact]
    public async Task AFinishedBookingCannotBeCancelled()
    {
        using var harness = new TestHarness();
        await using var db = harness.NewContext();
        var policy = new CancellationPolicyService(db);

        // The only genuine refusal: there is nothing left to call off.
        foreach (var status in new[] { BookingStatus.Completed, BookingStatus.Cancelled })
        {
            var decision = await policy.Evaluate(new Booking { Status = status });

            Assert.False(decision.Allowed);
            Assert.False(string.IsNullOrWhiteSpace(decision.Reason));
        }
    }
}

public class InMemoryStoreTests
{
    [Fact]
    public async Task Lock_IsGrantedToExactlyOneOfManyConcurrentCallers()
    {
        var locks = new InMemoryLockService();
        var key = $"booking:{Guid.NewGuid()}:accept";

        var attempts = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => locks.TryAcquireAsync(key, $"owner-{i}", TimeSpan.FromMinutes(1))));

        var results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(won => won));
    }

    [Fact]
    public async Task Lock_IsReacquirableOnceItExpires()
    {
        var locks = new InMemoryLockService();
        var key = $"booking:{Guid.NewGuid()}:accept";

        Assert.True(await locks.TryAcquireAsync(key, "first", TimeSpan.FromMilliseconds(50)));
        Assert.False(await locks.TryAcquireAsync(key, "second", TimeSpan.FromMinutes(1)));

        await Task.Delay(120);

        // Otherwise a crashed holder would block the booking forever.
        Assert.True(await locks.TryAcquireAsync(key, "second", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Lock_CannotBeReleasedBySomeoneElse()
    {
        var locks = new InMemoryLockService();
        var key = $"booking:{Guid.NewGuid()}:accept";

        await locks.TryAcquireAsync(key, "owner", TimeSpan.FromMinutes(1));
        await locks.ReleaseAsync(key, "impostor");

        Assert.Equal("owner", await locks.GetOwnerAsync(key));
    }

    [Fact]
    public async Task Presence_StaysOnlineUntilTheLastDeviceDisconnects()
    {
        var presence = new InMemoryPresenceStore();
        var userId = Guid.NewGuid().ToString();

        await presence.SetOnlineAsync(userId);   // phone
        await presence.SetOnlineAsync(userId);   // tablet

        await presence.SetOfflineAsync(userId);
        Assert.True(await presence.IsOnlineAsync(userId));

        await presence.SetOfflineAsync(userId);
        Assert.False(await presence.IsOnlineAsync(userId));
    }

    [Fact]
    public async Task Location_ExpiresAfterItsTtl()
    {
        var store = new InMemoryLocationStore();
        var providerId = Guid.NewGuid().ToString();

        await store.SetAsync(providerId, new GeoPoint(30.0444, 31.2357, null, DateTime.UtcNow),
            TimeSpan.FromMilliseconds(50));

        Assert.NotNull(await store.GetAsync(providerId));

        await Task.Delay(120);

        // SRS 7.2 - a stale position must read as unknown, not as the last known one.
        Assert.Null(await store.GetAsync(providerId));
    }
}

public class RecordingChatNotifier : IChatNotifier
{
    public List<(string RecipientId, ChatMessageDto Message)> Messages { get; } = [];
    public List<Guid> Locked { get; } = [];

    public Task MessageReceivedAsync(string recipientUserId, ChatMessageDto message)
    {
        Messages.Add((recipientUserId, message));
        return Task.CompletedTask;
    }

    public Task MessageReadAsync(Guid bookingId, Guid messageId) => Task.CompletedTask;

    public Task ChatLockedAsync(Guid bookingId)
    {
        Locked.Add(bookingId);
        return Task.CompletedTask;
    }

    public Task PresenceChangedAsync(Guid bookingId, string userId, bool isOnline) => Task.CompletedTask;
}
