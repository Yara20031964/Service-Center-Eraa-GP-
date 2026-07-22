using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services
{
    public class BookingAccessService : IBookingAccessService
    {
        private static readonly BookingStatus[] ClosedStatuses =
        [
            BookingStatus.Completed,
            BookingStatus.Cancelled,
            BookingStatus.NoProviderFound,
            BookingStatus.Failed,
        ];

        private readonly AppDbContext _db;

        public BookingAccessService(AppDbContext db) => _db = db;

        public async Task<BookingParticipants?> GetParticipantsAsync(Guid bookingId)
        {
            var row = await _db.Bookings
                .AsNoTracking()
                .Where(b => b.Id == bookingId)
                .Select(b => new { b.Id, b.CustomerId, b.ProviderId, b.Status })
                .FirstOrDefaultAsync();

            if (row is null) return null;

            return new BookingParticipants(
                row.Id,
                row.CustomerId,
                row.ProviderId,
                ClosedStatuses.Contains(row.Status));
        }

        public async Task<bool> IsParticipantAsync(Guid bookingId, string userId)
        {
            var p = await GetParticipantsAsync(bookingId);
            if (p is null) return false;
            return p.CustomerId == userId || (p.ProviderId is not null && p.ProviderId == userId);
        }
    }
}
