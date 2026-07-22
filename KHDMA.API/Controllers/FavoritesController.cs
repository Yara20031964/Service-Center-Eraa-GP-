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
    public class FavoritesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FavoritesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("{providerId}")]
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
        public async Task<IActionResult> Get()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(ApiResponse<List<ProviderDto>>.Unauthorized());

            var query = new GetFavoriteProvidersQuery
            {
                CustomerId = userId
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
