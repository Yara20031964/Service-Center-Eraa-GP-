using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.DTOs;
using KHDMA.Application.Interfaces.Services;

namespace KHDMA.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "Admin")]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] SendBroadcastDto dto)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        [HttpPost("send/{userId}")]
        public async Task<IActionResult> SendToUser(string userId, [FromBody] SendBroadcastDto dto)
        {
            // TODO: implement
            throw new NotImplementedException();
        }
    }
}