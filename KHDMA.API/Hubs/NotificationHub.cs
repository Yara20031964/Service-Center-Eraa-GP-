using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace KHDMA.API.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
          
            await base.OnConnectedAsync();
        }
    }

}
