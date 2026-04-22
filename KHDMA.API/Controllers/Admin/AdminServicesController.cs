using KHDMA.Application.DTOs.Catalog;
using KHDMA.Application.Interfaces.Services.Admin;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers.Admin;

[ApiController]
[Route("api/admin/services")]
public class AdminServicesController : ControllerBase
{
    private readonly IAdminServiceService _service;

    public AdminServicesController(IAdminServiceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(search, categoryId, isActive, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateServiceDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateServiceDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _service.ToggleActiveAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}/images")]
    public async Task<IActionResult> GetImages(Guid id)
    {
        var result = await _service.GetImagesAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id}/images")]
    public async Task<IActionResult> AddImages(Guid id, [FromForm] List<IFormFile> images)
    {
        var result = await _service.AddImagesAsync(id, images);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("images/{imageId}")]
    public async Task<IActionResult> DeleteImage(Guid imageId)
    {
        var result = await _service.DeleteImageAsync(imageId);
        return StatusCode(result.StatusCode, result);
    }
}
