using Domain.Common;
using KHDMA.Application.DTOs;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Tags(ApiTags.CommonNotifications)]
    public class NotificationsController : ControllerBase
    {
        private readonly IGenericRepository<Notification> _notificationRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            IGenericRepository<Notification> notificationRepo,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _notificationRepo = notificationRepo;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");

        private static NotificationDto ToDto(Notification n) => new()
        {
            Id = n.Id,
            Title = n.Title,
            Body = n.Body,
            Type = n.Type,
            IsRead = n.IsRead,
            SentAt = n.SentAt,
        };

        [HttpPost("register-token")]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status501NotImplemented)]
        public Task<IActionResult> RegisterToken([FromBody] DeviceTokenDto dto)
        {
            // Push delivery is not wired up yet (no FCM credentials); notifications
            // are persisted and pushed over SignalR instead. Answering 501 in the
            // normal envelope beats throwing, which produced a bodiless 500.
            return Task.FromResult<IActionResult>(StatusCode(501,
                ApiResponse<bool>.Fail("Device token registration is not implemented yet", 501)));
        }

        /// <summary>
        /// Writes a notification into another user's inbox. Admin only - this used
        /// to be reachable without a token at all, which let anyone post anything
        /// into anyone's inbox.
        /// </summary>
        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        [Tags(ApiTags.AdminNotifications)]
        [ProducesResponseType<ApiResponse<NotificationDto>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<NotificationDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
        {
            var target = await _userManager.FindByIdAsync(dto.UserId);
            if (target is null)
                return NotFound(ApiResponse<NotificationDto>.NotFound("User does not exist"));

            var notification = new Notification
            {
                UserId = dto.UserId,
                BookingId = dto.BookingId,
                Title = dto.Title,
                Body = dto.Body,
                Type = dto.Type,
            };

            await _notificationRepo.CreateAsync(notification);
            await _unitOfWork.CommitAsync();

            return StatusCode(201, ApiResponse<NotificationDto>.Created(ToDto(notification)));
        }

        /// <summary>
        /// One user's inbox. You may only read your own unless you are an admin -
        /// this route previously required no token and accepted any user id.
        /// </summary>
        [HttpGet("user/{userId}")]
        [ProducesResponseType<ApiResponse<List<NotificationDto>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<List<NotificationDto>>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<List<NotificationDto>>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<List<NotificationDto>>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserNotifications(string userId)
        {
            if (UserId is null) return Unauthorized(ApiResponse<List<NotificationDto>>.Unauthorized());
            if (!IsAdmin && userId != UserId)
                return StatusCode(403, ApiResponse<List<NotificationDto>>.Forbidden("You can only read your own notifications"));

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return NotFound(ApiResponse<List<NotificationDto>>.NotFound("User does not exist"));

            var result = await _notificationRepo.GetAsync(e => e.UserId == user.Id, tracked: false);

            // An empty inbox is a 200 with an empty array, not a 404 - the client
            // renders "no notifications" from the list, it should not have to treat
            // an ordinary empty state as an error.
            var data = result.OrderByDescending(n => n.SentAt).Select(ToDto).ToList();
            return Ok(ApiResponse<List<NotificationDto>>.Ok(data));
        }

        [HttpGet]
        [ProducesResponseType<PagedResponse<NotificationDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<PagedResponse<NotificationDto>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] string? type,
            [FromQuery] bool? isRead,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (UserId is null) return Unauthorized(PagedResponse<NotificationDto>.Fail("Unauthorized", 401));

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var result = await _notificationRepo.GetAsync(e => e.UserId == UserId, tracked: false);

            if (!string.IsNullOrEmpty(type))
                result = result.Where(e => e.Type == type);

            if (isRead.HasValue)
                result = result.Where(e => e.IsRead == isRead);

            var totalCount = result.Count();

            var data = result
                .OrderByDescending(n => n.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToDto)
                .ToList();

            return Ok(PagedResponse<NotificationDto>.Ok(data, totalCount, page, pageSize));
        }

        [HttpPut("{id}/read")]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Read(Guid id)
        {
            var notification = await _notificationRepo.GetOneAsync(e => e.Id == id);

            if (notification is null)
                return NotFound(ApiResponse<bool>.NotFound("Notification does not exist"));

            // Ownership was never checked here: any signed-in user could mark or
            // delete somebody else's notification just by knowing its id.
            if (!IsAdmin && notification.UserId != UserId)
                return StatusCode(403, ApiResponse<bool>.Forbidden());

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _unitOfWork.CommitAsync();

            return Ok(ApiResponse<bool>.Ok(true, "Notification marked as read"));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var notification = await _notificationRepo.GetOneAsync(e => e.Id == id);

            if (notification is null)
                return NotFound(ApiResponse<bool>.NotFound("Notification does not exist"));

            if (!IsAdmin && notification.UserId != UserId)
                return StatusCode(403, ApiResponse<bool>.Forbidden());

            _notificationRepo.Delete(notification);
            await _unitOfWork.CommitAsync();

            return Ok(ApiResponse<bool>.Ok(true, "Notification deleted successfully"));
        }

        [HttpPut("read-all")]
        [ProducesResponseType<ApiResponse<int>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<int>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (UserId is null) return Unauthorized(ApiResponse<int>.Unauthorized());

            var notifications = await _notificationRepo.GetAsync(e => e.UserId == UserId && !e.IsRead);

            var now = DateTime.UtcNow;
            var count = 0;
            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.ReadAt = now;
                count++;
            }

            if (count > 0) await _unitOfWork.CommitAsync();

            // Nothing unread is a success, not a 404.
            return Ok(ApiResponse<int>.Ok(count, $"{count} notification(s) marked as read"));
        }
    }
}
