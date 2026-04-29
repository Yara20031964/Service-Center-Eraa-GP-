using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace KHDMA.Infrastructure.Hubs
{
    public class BookingHub : Hub
    {
        public async Task JoinBookingGroup(string bookingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, bookingId);
        }

        public async Task LeaveBookingGroup(string bookingId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, bookingId);
        }

        public async Task SendStatusUpdate(string bookingId, string status, string? eta)
        {
            await Clients.Group(bookingId).SendAsync("ReceiveStatusUpdate", bookingId, status, eta);
        }
    }
}
