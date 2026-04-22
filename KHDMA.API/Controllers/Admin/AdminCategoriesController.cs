using KHDMA.Application.DTOs.Catalog;
using KHDMA.Application.Interfaces.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KHDMA.API.Controllers.Admin;

[ApiController]
[Route("api/admin/categories")]
public class AdminCategoriesController : ControllerBase
{
    private readonly IAdminCategoryService _service;

    public AdminCategoriesController(IAdminCategoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(search, isActive, page, pageSize);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
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
}
