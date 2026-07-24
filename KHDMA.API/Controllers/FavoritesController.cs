using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KHDMA.Application.Features.Favorites.Commands.ToggleFavorite;
using KHDMA.Application.Features.Favorites.Queries.GetFavoriteProviders;
using System.Security.Claims;
using Domain.Common;
using Application.DTOs.Admin;

namespace KHDMA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Tags(ApiTags.CustomerFavorites)]
    public class FavoritesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FavoritesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("{providerId}")]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<bool>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Toggle(string providerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<bool>.Unauthorized());

            var command = new ToggleFavoriteProviderCommand
            {
                CustomerId = userId,
                ProviderId = providerId
            };

            var isAdded = await _mediator.Send(command);
            var message = isAdded ? "Provider added to favorites" : "Provider removed from favorites";
            return Ok(ApiResponse<bool>.Ok(isAdded, message));
        }

        [HttpGet]
        [ProducesResponseType<ApiResponse<List<ProviderDto>>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ApiResponse<List<ProviderDto>>>(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<List<ProviderDto>>.Unauthorized());

            var query = new GetFavoriteProvidersQuery
            {
                CustomerId = userId
            };

            var result = await _mediator.Send(query);
            // Ok() would pin the HTTP status to 200 even when the envelope says 404.
            return StatusCode(result.StatusCode, result);
        }
    }
}
