using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.Features.Reviews.Commands.CreateReview;
using KHDMA.Application.Features.Reviews.Commands.UpdateReview;
using KHDMA.Application.Features.Reviews.Commands.ReplyToReview;
using KHDMA.Application.Features.Reviews.Queries.GetProviderReviews;
using System.Security.Claims;
using Domain.Common;
using KHDMA.Application.DTOs.Review;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReviewsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<Guid>.Unauthorized());

            var command = new CreateReviewCommand
            {
                BookingId = dto.BookingId,
                CustomerId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                PunctualityRating = dto.PunctualityRating,
                WorkQualityRating = dto.WorkQualityRating,
                CleanlinesRating = dto.CleanlinessRating
            };

            var id = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetByProvider), new { providerId = userId }, ApiResponse<Guid>.Created(id));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new UpdateReviewCommand
            {
                ReviewId = id,
                CustomerId = userId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                PunctualityRating = dto.PunctualityRating,
                WorkQualityRating = dto.WorkQualityRating,
                CleanlinesRating = dto.CleanlinessRating
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Review updated successfully")) : BadRequest(ApiResponse<bool>.Fail("Failed to update review"));
        }

        [HttpPost("{id}/reply")]
        public async Task<IActionResult> Reply(Guid id, [FromBody] string reply)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new ReplyToReviewCommand
            {
                ReviewId = id,
                ProviderId = userId,
                Reply = reply
            };

            var result = await _mediator.Send(command);
            return result ? Ok(ApiResponse<bool>.Ok(true, "Reply added successfully")) : BadRequest(ApiResponse<bool>.Fail("Failed to add reply"));
        }

        [HttpGet("provider/{providerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByProvider(string providerId, [FromQuery] int page = 1)
        {
            var query = new GetProviderReviewsQuery
            {
                ProviderId = providerId,
                Page = page
            };

            var result = await _mediator.Send(query);
            return Ok(result); // result is already PagedResponse
        }
    }
}
