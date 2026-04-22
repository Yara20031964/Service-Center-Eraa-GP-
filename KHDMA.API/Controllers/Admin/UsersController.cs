using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.DTOs;
namespace KHDMA.API.Controllers.Admin
{
    [ApiController] 
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        [HttpPut("notification-preferences")]
        public async Task<IActionResult> UpdateNotificationPreferences([FromBody] NotificationPreferencesDto dto)
        {
            // TODO: save preferences to DB
            throw new NotImplementedException();
        }
    }
} 
