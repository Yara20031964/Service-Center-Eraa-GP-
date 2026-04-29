using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Enums;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KHDMA.API.Controllers
{
    [Route("api/admin/bookings")]
    [ApiController]
     [Authorize(Roles = "Admin")] // will be  uncommented if Auth is setup
    public class AdminBookingsController : ControllerBase
    {
        private readonly IAdminBookingService _bookingService;

        public AdminBookingsController(IAdminBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBookings(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] BookingStatus? status = null,
            [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null,
            [FromQuery] string? customerId = null, [FromQuery] string? providerId = null)
        {
            var response = await _bookingService.GetAllBookingsAsync(page, pageSize, status, fromDate, toDate, customerId, providerId);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookingDetails(Guid id)
        {
            var response = await _bookingService.GetBookingDetailsAsync(id);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(Guid id, [FromBody] string reason)
        {
            var response = await _bookingService.CancelBookingAsync(id, reason);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetBookingHistory(Guid id)
        {
            var response = await _bookingService.GetBookingStatusHistoryAsync(id);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }

        [HttpGet("{id}/transcript")]
        public async Task<IActionResult> GetChatTranscript(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var response = await _bookingService.GetChatTranscriptAsync(id, page, pageSize);
            return Ok(response);
        }
    }
}
