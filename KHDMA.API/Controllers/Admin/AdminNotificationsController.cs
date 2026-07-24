using Microsoft.AspNetCore.Authorization;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.DTOs;
using KHDMA.Application.Interfaces.Services;

namespace KHDMA.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/notifications")]
    [Authorize(Roles = "Admin")]
    [Tags(ApiTags.AdminNotifications)]
    public class AdminNotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public AdminNotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("broadcast")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Broadcast([FromBody] SendBroadcastDto dto)
        {
            // TODO: implement
            throw new NotImplementedException();
        }

        [HttpPost("send/{userId}")]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendToUser(string userId, [FromBody] SendBroadcastDto dto)
        {
            // TODO: implement
            throw new NotImplementedException();
        }
    }
}
