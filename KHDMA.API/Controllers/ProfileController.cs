using System.Security.Claims;
using KHDMA.Application.DTOs.Profile;
using KHDMA.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _service;

    public ProfileController(IProfileService service)
    {
        _service = service;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _service.GetProfileAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
    {
        var result = await _service.UpdateProfileAsync(UserId, dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var result = await _service.ChangePasswordAsync(UserId, dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetAddresses()
    {
        var result = await _service.GetAddressesAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] CreateAddressDto dto)
    {
        var result = await _service.AddAddressAsync(UserId, dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("addresses/{addressId}")]
    public async Task<IActionResult> DeleteAddress(Guid addressId)
    {
        var result = await _service.DeleteAddressAsync(UserId, addressId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("certificates")]
    public async Task<IActionResult> GetCertificateImages()
    {
        var result = await _service.GetCertificateImagesAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("certificates")]
    public async Task<IActionResult> AddCertificateImages([FromForm] List<IFormFile> images)
    {
        var result = await _service.AddCertificateImagesAsync(UserId, images);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("certificates/{imageId}")]
    public async Task<IActionResult> DeleteCertificateImage(Guid imageId)
    {
        var result = await _service.DeleteCertificateImageAsync(UserId, imageId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("portfolio")]
    public async Task<IActionResult> GetPortfolioImages()
    {
        var result = await _service.GetPortfolioImagesAsync(UserId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("portfolio")]
    public async Task<IActionResult> AddPortfolioImages([FromForm] List<IFormFile> images)
    {
        var result = await _service.AddPortfolioImagesAsync(UserId, images);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("portfolio/{imageId}")]
    public async Task<IActionResult> DeletePortfolioImage(Guid imageId)
    {
        var result = await _service.DeletePortfolioImageAsync(UserId, imageId);
        return StatusCode(result.StatusCode, result);
    }
}
