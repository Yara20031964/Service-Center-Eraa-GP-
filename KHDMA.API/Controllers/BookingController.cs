using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KHDMA.Application.Features.Bookings.Commands.CreateBooking;
using KHDMA.Application.Features.Bookings.Commands.CancelBooking;
using KHDMA.Application.Features.Bookings.Queries.GetBookingHistory;
using KHDMA.Application.Features.Bookings.Queries.GetAdminBookings;
using KHDMA.Application.Features.Bookings.Queries.ExportBookings;
using System.Security.Claims;
using KHDMA.Application.DTOs.Booking;
using Domain.Common;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BookingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<Guid>.Unauthorized());

            var command = new CreateBookingCommand
            {
                CustomerId = userId,
                ProviderId = dto.ProviderId,
                ServiceId = dto.ServiceId,
                BookingType = dto.BookingType,
                ScheduledTime = dto.ScheduledTime,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TotalPrice = dto.TotalPrice,
                Notes = dto.Notes
            };

            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetHistory), new { id }, ApiResponse<Guid>.Created(id));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new CancelBookingCommand
            {
                BookingId = id,
                Reason = reason,
                UserId = userId,
                IsAdmin = false
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Cancelled successfully")) : NotFound(ApiResponse<bool>.NotFound());
        }

        [HttpPost("admin/{id}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminCancel(Guid id, [FromBody] string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var command = new CancelBookingCommand
            {
                BookingId = id,
                Reason = reason,
                UserId = userId!,
                IsAdmin = true
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Admin cancelled successfully")) : NotFound(ApiResponse<bool>.NotFound());
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<object>.Unauthorized());

            var query = new GetBookingHistoryQuery
            {
                UserId = userId,
                Status = status,
                FromDate = from,
                ToDate = to,
                Page = page
            };

            var result = await _mediator.Send(query);
            return Ok(result); // result is already PagedResponse
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminList([FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? customerId, [FromQuery] string? providerId, [FromQuery] int page = 1)
        {
            var query = new GetAdminBookingsQuery
            {
                Status = status,
                FromDate = from,
                ToDate = to,
                CustomerId = customerId,
                ProviderId = providerId,
                Page = page
            };

            var result = await _mediator.Send(query);
            return Ok(result); // result is already PagedResponse
        }

        [HttpGet("admin/export")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Export([FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string format = "csv")
        {
            var query = new ExportBookingsQuery
            {
                Status = status,
                FromDate = from,
                ToDate = to,
                Format = format
            };

            var result = await _mediator.Send(query);
            var fileName = $"bookings_{DateTime.Now:yyyyMMddHHmmss}.{(format.ToLower() == "excel" ? "xlsx" : "csv")}";
            var contentType = format.ToLower() == "excel" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv";
            
            return File(result, contentType, fileName);
        }
    }
}
