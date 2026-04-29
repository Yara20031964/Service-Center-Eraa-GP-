using System.Security.Claims;
using KHDMA.Application.DTOs.Auth.Request;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KHDMA.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("register/customer")]
    public async Task<IActionResult> RegisterCustomer([FromForm] RegisterCustomerDto dto)
    {
        var result = await _service.RegisterCustomerAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("register/provider")]
    public async Task<IActionResult> RegisterProvider([FromForm] RegisterProviderDto dto)
    {
        var result = await _service.RegisterProviderAsync(dto);
        if (!result.IsSuccess)
            return BadRequest(result);
        return Ok(result);
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _service.LoginAsync(dto);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [EnableRateLimiting("AuthPolicy")]
    [HttpPost("login/admin")]
    public async Task<IActionResult> AdminLogin([FromBody] LoginDto dto)
    {
        var result = await _service.AdminLoginAsync(dto);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _service.RefreshTokenAsync(dto.RefreshToken);
        if (!result.IsSuccess)
            return Unauthorized(result);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        await _service.RevokeTokenAsync(dto.RefreshToken);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPost("send-email-confirmation")]
    [Authorize]
    public async Task<IActionResult> SendEmailConfirmation()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await _service.SendEmailConfirmationAsync(userId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var result = await _service.ConfirmEmailAsync(userId, token);
        return StatusCode(result.StatusCode, result);
    }
}
