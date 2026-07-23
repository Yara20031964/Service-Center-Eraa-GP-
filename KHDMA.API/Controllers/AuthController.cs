using Domain.Common;
using System.Security.Claims;
using KHDMA.Application.DTOs.Auth.Request;
using KHDMA.Application.DTOs.Auth.Response;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KHDMA.API.Controllers;

[ApiController]
[Route("api/auth")]
[Tags(ApiTags.CommonAuth)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("register/customer")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterCustomer([FromForm] RegisterCustomerDto dto)
    {
        var result = await _service.RegisterCustomerAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("register/provider")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterProvider([FromForm] RegisterProviderDto dto)
    {
        var result = await _service.RegisterProviderAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _service.LoginAsync(dto);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("login/admin")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminLogin([FromBody] LoginDto dto)
    {
        var result = await _service.AdminLoginAsync(dto);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _service.RefreshTokenAsync(dto.RefreshToken);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        await _service.RevokeTokenAsync(dto.RefreshToken);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("send-email-confirmation")]
    [Authorize]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendEmailConfirmation()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _service.SendEmailConfirmationAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("confirm-email")]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<string>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var result = await _service.ConfirmEmailAsync(userId, token);
        return StatusCode(result.StatusCode, result);
    }
}
