using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using KHDMA.Application.Features.Bookings.Commands.CreateBooking;
using KHDMA.Application.Features.Bookings.Commands.CancelBooking;
using KHDMA.Application.Features.Bookings.Commands.CompleteBooking;
using KHDMA.Application.Features.Bookings.Queries.GetBookingHistory;
using KHDMA.Application.Features.Bookings.Queries.GetAdminBookings;
using KHDMA.Application.Features.Bookings.Queries.ExportBookings;
using System.Security.Claims;
using KHDMA.Application.DTOs.Booking;
using KHDMA.Application.DTOs.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Enums;
using Domain.Common;
using Application.DTOs.Payment;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    // Default section for this controller. The provider-side lifecycle actions and
    // the three admin-only ones override it with their own [Tags] below, because a
    // single controller here serves all three audiences.
    [Tags(ApiTags.CustomerBookings)]
    public class BookingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IBookingDetailsService _bookingDetails;
        private readonly IPricingService _pricing;

        public BookingController(
            IMediator mediator,
            IBookingDetailsService bookingDetails,
            IPricingService pricing)
        {
            _mediator = mediator;
            _bookingDetails = bookingDetails;
            _pricing = pricing;
        }

        /// <summary>
        /// What a service will cost, without creating anything.
        /// </summary>
        /// <remarks>
        /// The checkout screen used to obtain its figures by creating the booking,
        /// which meant the customer saw the price only after dispatch had already
        /// started broadcasting to providers - and backing out of that screen left a
        /// live booking behind. This returns the same numbers from the same
        /// <see cref="IPricingService"/> the create handler uses, so quoting and
        /// charging can never diverge.
        /// </remarks>
        [HttpGet("quote")]
        [ProducesResponseType<ApiResponse<PriceBreakdownDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<PriceBreakdownDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<PriceBreakdownDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Quote([FromQuery] Guid serviceId)
        {
            var price = await _pricing.ForServiceAsync(serviceId);
            if (price is null)
            {
                var missing = ApiResponse<PriceBreakdownDto>.NotFound("Service not found");
                return StatusCode(missing.StatusCode, missing);
            }

            var result = ApiResponse<PriceBreakdownDto>.Ok(new PriceBreakdownDto
            {
                ServiceFee = price.ServiceFee,
                VatRate = price.VatRate,
                VatAmount = price.VatAmount,
                Total = price.Total,
                Currency = price.Currency,
            });

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Dispatch booking (SRS 2.2): no provider is chosen; the backend broadcasts
        /// to nearby eligible providers and the first to accept wins.
        /// </summary>
        [HttpPost]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<CreateBookingResultDto>.Unauthorized());

            var result = await _mediator.Send(ToCommand(dto, userId, providerId: null));
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Direct booking: the customer picked this provider from their profile page.
        /// Offered to that provider only - it never falls back to a broadcast.
        /// </summary>
        [HttpPost("direct")]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status201Created)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<CreateBookingResultDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateDirect([FromBody] CreateDirectBookingDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<CreateBookingResultDto>.Unauthorized());

            if (string.IsNullOrWhiteSpace(dto.ProviderId))
                return BadRequest(ApiResponse<CreateBookingResultDto>.Fail("providerId is required"));

            var result = await _mediator.Send(ToCommand(dto, userId, dto.ProviderId));
            return StatusCode(result.StatusCode, result);
        }

        // Note: no price is copied from the request. The server snapshots
        // Service.FixedPrice - see CreateBookingDto.
        private static CreateBookingCommand ToCommand(CreateBookingDto dto, string customerId, string? providerId)
            => new()
            {
                CustomerId = customerId,
                ProviderId = providerId,
                ServiceId = dto.ServiceId,
                BookingType = dto.BookingType,
                ScheduledTime = dto.ScheduledTime,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                AddressId = dto.AddressId,
                Notes = dto.Notes,
            };

        [HttpGet("{id:guid}")]
        [Tags(ApiTags.CommonBookings)]
        [ProducesResponseType<ApiResponse<BookingDetailDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<BookingDetailDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<BookingDetailDto>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<BookingDetailDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                var unauthorized = ApiResponse<BookingDetailDto>.Unauthorized();
                return StatusCode(unauthorized.StatusCode, unauthorized);
            }

            var result = await _bookingDetails.GetAsync(id, userId);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Cancels the caller's own booking. Allowed at any point before the job
        /// closes; outside the free window the policy's flat fee is recorded on the
        /// booking and named in the response message.
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status404NotFound)]
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
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("admin/{id}/cancel")]
        [Authorize(Roles = "Admin")]
        [Tags(ApiTags.AdminBookings)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status404NotFound)]
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
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Bookings for whoever the token belongs to - the ones a customer raised,
        /// or the ones a provider was assigned.
        /// </summary>
        [HttpGet("history")]
        [Tags(ApiTags.CommonBookings)]
        [ProducesResponseType<PagedResponse<BookingListDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
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
        [Tags(ApiTags.AdminBookings)]
        [ProducesResponseType<PagedResponse<BookingListDto>>(StatusCodes.Status200OK)]
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

        [HttpPost("{id}/complete")]
        [Tags(ApiTags.ProviderJobs)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Complete(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new CompleteBookingCommand
            {
                BookingId = id,
                ProviderId = userId
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Booking completed successfully")) : BadRequest(ApiResponse<bool>.Fail("Failed to complete booking"));
        }

        /// <summary>
        /// Claim a dispatched job. Exactly one concurrent caller succeeds;
        /// the rest get 409 and should dismiss the card silently.
        /// </summary>
        [HttpPost("{id}/accept")]
        [Tags(ApiTags.ProviderJobs)]
        [ProducesResponseType<ApiResponse<AcceptResultDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<AcceptResultDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<AcceptResultDto>>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ApiResponse<AcceptResultDto>>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ApiResponse<AcceptResultDto>>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Accept(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(ApiResponse<AcceptResultDto>.Unauthorized());

            var command = new KHDMA.Application.Features.Bookings.Commands.AcceptBooking.AcceptBookingCommand
            {
                BookingId = id,
                ProviderId = userId
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/reject")]
        [Tags(ApiTags.ProviderJobs)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Reject(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new KHDMA.Application.Features.Bookings.Commands.RejectBooking.RejectBookingCommand
            {
                BookingId = id,
                ProviderId = userId
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Booking rejected successfully")) : BadRequest(ApiResponse<bool>.Fail("Failed to reject booking"));
        }

        [HttpPost("{id}/mark-en-route")]
        [Tags(ApiTags.ProviderJobs)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkEnRoute(Guid id, [FromQuery] string? eta)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new KHDMA.Application.Features.Bookings.Commands.UpdateStatus.UpdateBookingStatusCommand
            {
                BookingId = id,
                ProviderId = userId,
                NewStatus = BookingStatus.EnRoute,
                Eta = eta
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Changed status to En Route")) : BadRequest(ApiResponse<bool>.Fail("Failed to update status"));
        }

        [HttpPost("{id}/mark-arrived")]
        [Tags(ApiTags.ProviderJobs)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkArrived(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new KHDMA.Application.Features.Bookings.Commands.UpdateStatus.UpdateBookingStatusCommand
            {
                BookingId = id,
                ProviderId = userId,
                NewStatus = BookingStatus.Arrived
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Changed status to Arrived")) : BadRequest(ApiResponse<bool>.Fail("Failed to update status"));
        }

        [HttpPost("{id}/mark-in-progress")]
        [Tags(ApiTags.ProviderJobs)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkInProgress(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new KHDMA.Application.Features.Bookings.Commands.UpdateStatus.UpdateBookingStatusCommand
            {
                BookingId = id,
                ProviderId = userId,
                NewStatus = BookingStatus.InProgress
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Changed status to In Progress")) : BadRequest(ApiResponse<bool>.Fail("Failed to update status"));
        }

        [HttpGet("admin/export")]
        [Authorize(Roles = "Admin")]
        [Tags(ApiTags.AdminBookings)]
        [Produces("text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [ProducesResponseType<FileResult>(StatusCodes.Status200OK)]
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
        [HttpPost("{id}/retry-payment")]
        [ProducesResponseType<ApiResponse<PaymentKeyResponseDto>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<PaymentKeyResponseDto>>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ApiResponse<PaymentKeyResponseDto>>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ApiResponse<PaymentKeyResponseDto>>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RetryPayment(Guid id)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(customerId))
                return Unauthorized(ApiResponse<PaymentKeyResponseDto>.Unauthorized());

            var command = new KHDMA.Application.Features.Bookings.Commands.RetryPayment.RetryPaymentCommand
            {
                BookingId = id,
                CustomerId = customerId,
            };
            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}
