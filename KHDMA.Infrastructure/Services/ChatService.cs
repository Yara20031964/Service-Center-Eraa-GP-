using Domain.Common;
using KHDMA.Application.Common;
using KHDMA.Application.DTOs.Admin;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;
        private readonly IBookingAccessService _access;
        private readonly IChatNotifier _notifier;
        private readonly IPresenceStore _presence;

        public ChatService(
            AppDbContext db,
            IBookingAccessService access,
            IChatNotifier notifier,
            IPresenceStore presence)
        {
            _db = db;
            _access = access;
            _notifier = notifier;
            _presence = presence;
        }

        public async Task<ApiResponse<ChatMessageDto>> SendMessageAsync(Guid bookingId, string senderId, SendMessageDto dto)
        {
            var participants = await _access.GetParticipantsAsync(bookingId);
            if (participants is null)
                return ApiResponse<ChatMessageDto>.NotFound("Booking not found");

            if (participants.CustomerId != senderId && participants.ProviderId != senderId)
                return ApiResponse<ChatMessageDto>.Forbidden("You are not a participant in this booking");

            // Auto-lock: history stays readable, but the conversation is closed.
            if (participants.IsClosed)
            {
                await _notifier.ChatLockedAsync(bookingId);
                return ApiResponse<ChatMessageDto>.Fail("This conversation is closed because the booking has ended.", 409);
            }

            var type = (dto.MessageType ?? "Text").Trim();

            if (string.Equals(type, "Text", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(dto.MessageText))
                    return ApiResponse<ChatMessageDto>.Fail("Message text is required");

                // SRS 7.3 - contact details must not leave the platform.
                if (PiiFilter.ContainsPii(dto.MessageText, out var reason))
                    return ApiResponse<ChatMessageDto>.Fail(reason, 400);
            }
            else if (string.Equals(type, "Image", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(dto.AttachmentUrl))
                    return ApiResponse<ChatMessageDto>.Fail("An uploaded attachment url is required for image messages");
            }
            else if (!string.Equals(type, "QuickReply", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<ChatMessageDto>.Fail("messageType must be Text, Image or QuickReply");
            }

            var entity = new ChatMessage
            {
                BookingId = bookingId,
                SenderId = senderId,
                MessageType = type,
                MessageText = dto.MessageText,
                AttachmentUrl = dto.AttachmentUrl,
                SentAt = DateTime.UtcNow,
                IsRead = false,
            };

            // Persist before broadcasting. If the broadcast fails the peer still
            // finds the message on reconnect; the reverse order would lose it.
            _db.ChatMessages.Add(entity);
            await _db.SaveChangesAsync();

            var senderName = await _db.Users
                .Where(u => u.Id == senderId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync();

            // Delivered to each participant separately: IsMine is per-viewer, and
            // one group send would serialize a single value for both of them.
            // Sending to the sender too keeps their other devices in sync.
            var peerId = participants.CustomerId == senderId
                ? participants.ProviderId
                : participants.CustomerId;

            await _notifier.MessageReceivedAsync(senderId, Map(entity, senderName, isMine: true));
            if (peerId is not null)
                await _notifier.MessageReceivedAsync(peerId, Map(entity, senderName, isMine: false));

            return ApiResponse<ChatMessageDto>.Created(Map(entity, senderName, isMine: true), "Message sent");
        }

        public async Task<PagedResponse<ChatMessageDto>> GetHistoryAsync(Guid bookingId, string userId, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            if (!await _access.IsParticipantAsync(bookingId, userId))
                return PagedResponse<ChatMessageDto>.Fail("You are not a participant in this booking", 403);

            var query = _db.ChatMessages.AsNoTracking().Where(m => m.BookingId == bookingId);
            var total = await query.CountAsync();

            // Newest first for paging, then reversed so the caller gets chronological
            // order within the page - which is how the message list renders.
            var rows = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id, m.BookingId, m.SenderId, m.MessageType,
                    m.MessageText, m.AttachmentUrl, m.SentAt, m.IsRead,
                    SenderName = m.Sender.FullName,
                })
                .ToListAsync();

            var data = rows
                .AsEnumerable()
                .Reverse()
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    BookingId = m.BookingId,
                    SenderId = m.SenderId,
                    SenderName = m.SenderName,
                    IsMine = m.SenderId == userId,
                    MessageType = m.MessageType,
                    MessageText = m.MessageText,
                    AttachmentUrl = m.AttachmentUrl,
                    SentAt = m.SentAt,
                    IsRead = m.IsRead,
                })
                .ToList();

            return PagedResponse<ChatMessageDto>.Ok(data, total, page, pageSize);
        }

        public async Task<ApiResponse<List<ChatThreadDto>>> GetThreadsAsync(string userId)
        {
            var closed = new[]
            {
                BookingStatus.Completed, BookingStatus.Cancelled,
                BookingStatus.NoProviderFound, BookingStatus.Failed,
            };

            // Only bookings that actually have a counterparty can have a thread.
            var bookings = await _db.Bookings
                .AsNoTracking()
                .Where(b => (b.CustomerId == userId || b.ProviderId == userId) && b.ProviderId != null)
                .Select(b => new
                {
                    b.Id,
                    b.CustomerId,
                    b.ProviderId,
                    b.Status,
                    b.ScheduledTime,
                    b.CreateAt,
                    ServiceNameEn = b.Service.NameEn,
                    ServiceNameAr = b.Service.NameAr,
                    CustomerName = b.Customer.ApplicationUser.FullName,
                    CustomerAvatar = b.Customer.ApplicationUser.ProfilePictureUrl,
                    ProviderName = b.Provider!.ApplicationUser.FullName,
                    ProviderAvatar = b.Provider.ApplicationUser.ProfilePictureUrl,
                    ProviderJobTitle = b.Provider.JobTitle,
                    LastMessage = b.ChatMessages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new { m.MessageText, m.MessageType, m.SentAt })
                        .FirstOrDefault(),
                    UnreadCount = b.ChatMessages.Count(m => !m.IsRead && m.SenderId != userId),
                })
                .ToListAsync();

            var threads = new List<ChatThreadDto>(bookings.Count);

            foreach (var b in bookings)
            {
                var viewerIsCustomer = b.CustomerId == userId;
                var peerId = viewerIsCustomer ? b.ProviderId! : b.CustomerId;

                var when = b.ScheduledTime ?? b.CreateAt;
                var chip = $"{b.ServiceNameEn.ToUpperInvariant()} — {when:ddd d MMM h:mm tt}";

                threads.Add(new ChatThreadDto
                {
                    BookingId = b.Id,
                    PeerId = peerId,
                    PeerName = viewerIsCustomer ? b.ProviderName : b.CustomerName,
                    PeerRoleEn = viewerIsCustomer ? (b.ProviderJobTitle ?? b.ServiceNameEn) : "Customer",
                    PeerRoleAr = viewerIsCustomer ? b.ServiceNameAr : "عميل",
                    PeerAvatarUrl = viewerIsCustomer ? b.ProviderAvatar : b.CustomerAvatar,
                    LastMessage = b.LastMessage?.MessageType == "Image" ? "📷 Photo" : b.LastMessage?.MessageText,
                    LastMessageAt = b.LastMessage?.SentAt,
                    UnreadCount = b.UnreadCount,
                    IsOnline = await _presence.IsOnlineAsync(peerId),
                    IsLocked = closed.Contains(b.Status),
                    ScheduleChipEn = chip,
                    ScheduleChipAr = $"{b.ServiceNameAr} — {when:ddd d MMM h:mm tt}",
                });
            }

            var ordered = threads
                .OrderByDescending(t => t.LastMessageAt ?? DateTime.MinValue)
                .ToList();

            return ApiResponse<List<ChatThreadDto>>.Ok(ordered);
        }

        public async Task<ApiResponse<int>> MarkReadAsync(Guid bookingId, string userId)
        {
            if (!await _access.IsParticipantAsync(bookingId, userId))
                return ApiResponse<int>.Forbidden("You are not a participant in this booking");

            // Only the peer's messages get marked - you cannot "read" your own.
            var updated = await _db.ChatMessages
                .Where(m => m.BookingId == bookingId && m.SenderId != userId && !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

            if (updated > 0)
            {
                var lastId = await _db.ChatMessages
                    .Where(m => m.BookingId == bookingId && m.SenderId != userId)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.Id)
                    .FirstOrDefaultAsync();

                await _notifier.MessageReadAsync(bookingId, lastId);
            }

            return ApiResponse<int>.Ok(updated, $"{updated} message(s) marked read");
        }

        public async Task<ApiResponse<List<ChatTranscriptDto>>> GetTranscriptAsync(Guid bookingId)
        {
            var exists = await _db.Bookings.AnyAsync(b => b.Id == bookingId);
            if (!exists) return ApiResponse<List<ChatTranscriptDto>>.NotFound("Booking not found");

            var rows = await _db.ChatMessages
                .AsNoTracking()
                .Where(m => m.BookingId == bookingId)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatTranscriptDto
                {
                    MessageId = m.Id,
                    SenderName = m.Sender.FullName,
                    MessageText = m.MessageText ?? string.Empty,
                    MessageType = m.MessageType,
                    SentAt = m.SentAt,
                    AttachmentUrl = m.AttachmentUrl,
                })
                .ToListAsync();

            return ApiResponse<List<ChatTranscriptDto>>.Ok(rows);
        }

        private static ChatMessageDto Map(ChatMessage m, string? senderName, bool isMine) => new()
        {
            Id = m.Id,
            BookingId = m.BookingId,
            SenderId = m.SenderId,
            SenderName = senderName,
            IsMine = isMine,
            MessageType = m.MessageType,
            MessageText = m.MessageText,
            AttachmentUrl = m.AttachmentUrl,
            SentAt = m.SentAt,
            IsRead = m.IsRead,
        };
    }
}
