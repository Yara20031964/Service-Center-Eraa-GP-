using KHDMA.Application.DTOs;
using KHDMA.Application.DTOs.Responses;
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
    public class NotificationsController : ControllerBase
    {
        IGenericRepository<Notification> _notificationRepo;
        IUnitOfWork  _unitOfWork;
        UserManager<ApplicationUser> _UserManager;
        public NotificationsController(IGenericRepository<Notification> notificationRepo, IUnitOfWork unitOfWork, UserManager<ApplicationUser> UserManager)
        {
            _notificationRepo = notificationRepo;
            _unitOfWork = unitOfWork;
            _UserManager = UserManager;
        }
        [HttpPost("register-token")]
        [Authorize]
        public async Task<IActionResult> RegisterToken([FromBody] DeviceTokenDto dto)
        {
            // TODO: save token to DB
            throw new NotImplementedException();
        }




        [HttpPost("send")]
        public async Task<IActionResult> SendNotification(SendNotificationDto sendNotificationDto)
        {
            var notification = new Notification
            {
                UserId = sendNotificationDto.UserId,
                BookingId = sendNotificationDto.BookingId,
                Title = sendNotificationDto.Title,
                Body = sendNotificationDto.Body,
                Type = sendNotificationDto.Type

            };
           await _notificationRepo.CreateAsync(notification);
            await _unitOfWork.CommitAsync();
            return Ok(notification);
        }
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(string userId)
        {
            var user = await _UserManager.FindByIdAsync(userId);

            if (user is null)
                return NotFound(new ErrorModel
                {
                    Code = "UserNotFound",
                    Description = "User does not exist"
                });

            var result = await _notificationRepo.GetAsync(e => e.UserId == user.Id);

            if (!result.Any())
                return NotFound(new ErrorModel
                {
                    Code = "NoNotifications",
                    Description = "No notifications found for this user"
                });

            return Ok(result);
        }
        [HttpPut("{id}/read")]
        [Authorize]
        public async Task<IActionResult> Read(Guid id)
        {
            var notification = await _notificationRepo.GetOneAsync(e => e.Id == id);

            if (notification is null)
                return NotFound(new ErrorModel
                {
                    Code = "NotificationNotFound",
                    Description = "Notification does not exist"
                });

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _unitOfWork.CommitAsync();

            return Ok(new SuccessModel
            {
                Msg = "Notification marked as read"
            });
        }
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var notification = await _notificationRepo.GetOneAsync(e => e.Id == id);

            if (notification is null)
                return NotFound(new ErrorModel
                {
                    Code = "NotificationNotFound",
                    Description = "Notification does not exist"
                });

            _notificationRepo.Delete(notification);
            await _unitOfWork.CommitAsync();

            return Ok(new SuccessModel
            {
                Msg = "Notification deleted successfully"
            });
        }
        [HttpPut("read-all")]
        [Authorize]
        public async Task<IActionResult> MarkAllAsRead( )
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();
            var notifications = await _notificationRepo.GetAsync(e => e.UserId == userId && !e.IsRead);

            if (!notifications.Any())
                return NotFound(new ErrorModel
                {
                    Code = "NoNotifications",
                    Description = "No unread notifications found"
                });

            foreach (var n in notifications)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }

            await _unitOfWork.CommitAsync();

            return Ok(new SuccessModel
            {
                Msg = "All notifications marked as read"
            });
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetNotifications(
              string? type,
              bool? isRead,
              int page = 1,
             int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var result = await _notificationRepo.GetAsync(e => e.UserId == userId, tracked: false);

            if (!string.IsNullOrEmpty(type))
                result = result.Where(e => e.Type == type);

            if (isRead.HasValue)
                result = result.Where(e => e.IsRead == isRead);

            var totalCount = result.Count();

            var data = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Data = data
            });
        }

    }



    
}
