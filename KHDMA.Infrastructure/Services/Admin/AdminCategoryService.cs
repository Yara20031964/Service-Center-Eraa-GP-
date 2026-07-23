using Domain.Common;
using KHDMA.Application.DTOs.Catalog;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Entities;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services.Admin;

public class AdminCategoryService : IAdminCategoryService
{
    private readonly AppDbContext _context;
    private readonly IImageUrlResolver _imageUrlResolver;

    public AdminCategoryService(AppDbContext context, IImageUrlResolver imageUrlResolver)
    {
        _context = context;
        _imageUrlResolver = imageUrlResolver;
    }

    public async Task<PagedResponse<CategoryDto>> GetAllAsync(string? search, bool? isActive, int page, int pageSize)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.NameEn.Contains(search) || c.NameAr.Contains(search));

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResponse<CategoryDto>.Ok(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<ApiResponse<CategoryDto>> GetByIdAsync(Guid id)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.id == id);
        if (category == null)
            return ApiResponse<CategoryDto>.NotFound("Category not found");

        return ApiResponse<CategoryDto>.Ok(MapToDto(category));
    }

    public async Task<ApiResponse<CategoryDto>> CreateAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            id = Guid.NewGuid(),
            NameEn = dto.NameEn,
            NameAr = dto.NameAr,
            Description = dto.Description,
            IconUrl = dto.IconUrl,
            IsActive = dto.IsActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return ApiResponse<CategoryDto>.Created(MapToDto(category));
    }

    public async Task<ApiResponse<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.id == id);
        if (category == null)
            return ApiResponse<CategoryDto>.NotFound("Category not found");

        if (dto.NameEn != null) category.NameEn = dto.NameEn;
        if (dto.NameAr != null) category.NameAr = dto.NameAr;
        if (dto.Description != null) category.Description = dto.Description;
        if (dto.IconUrl != null) category.IconUrl = dto.IconUrl;
        if (dto.IsActive.HasValue) category.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();
        return ApiResponse<CategoryDto>.Ok(MapToDto(category));
    }

    public async Task<ApiResponse<string>> ToggleActiveAsync(Guid id)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.id == id);
        if (category == null)
            return ApiResponse<string>.NotFound("Category not found");

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok(category.IsActive ? "Category activated" : "Category deactivated");
    }

    private CategoryDto MapToDto(Category c) => new()
    {
        Id = c.id,
        NameEn = c.NameEn,
        NameAr = c.NameAr,
        Description = c.Description,
        IconUrl = _imageUrlResolver.Resolve(c.IconUrl),
        IsActive = c.IsActive
    };
}
