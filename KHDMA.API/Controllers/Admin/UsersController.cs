using Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.DTOs;
namespace KHDMA.API.Controllers.Admin
{
    [ApiController] 
    [Route("api/users")]
    [Authorize]
    [Tags(ApiTags.CommonNotifications)]
    public class UsersController : ControllerBase
    {
        // Preferences are not modelled yet - answer 501 in the standard envelope
        // instead of throwing, which produced a bodiless 500.
        [HttpPut("notification-preferences")]
        public Task<IActionResult> UpdateNotificationPreferences([FromBody] NotificationPreferencesDto dto)
            => Task.FromResult<IActionResult>(StatusCode(501,
                ApiResponse<bool>.Fail("Notification preferences are not implemented yet", 501)));
    }
} 
