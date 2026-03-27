using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.Interfaces.Services.Admin;
using System;
using System.Threading.Tasks;

namespace KHDMA.API.Controllers
{
    [Route("api/admin/reviews")]
    [ApiController]
    // [Authorize(Roles = "Admin")]
    public class AdminReviewsController : ControllerBase
    {
        private readonly IAdminReviewService _reviewService;

        public AdminReviewsController(IAdminReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllReviews(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string? providerId = null, [FromQuery] string? customerId = null,
            [FromQuery] int? minRating = null, [FromQuery] int? maxRating = null)
        {
            var response = await _reviewService.GetAllReviewsAsync(page, pageSize, providerId, customerId, minRating, maxRating);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewDetails(Guid id)
        {
            var response = await _reviewService.GetReviewDetailsAsync(id);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateReviewStatus(Guid id, [FromQuery] bool isDeleted, [FromQuery] bool isHidden)
        {
            var response = await _reviewService.HideOrDeleteReviewAsync(id, isDeleted, isHidden);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }
}
