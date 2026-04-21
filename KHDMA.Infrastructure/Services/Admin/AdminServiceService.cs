using Domain.Common;
using KHDMA.Application.DTOs.Catalog;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Entities;
using KHDMA.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services.Admin;

public class AdminServiceService : IAdminServiceService
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public AdminServiceService(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<PagedResponse<ServiceDto>> GetAllAsync(string? search, Guid? categoryId, bool? isActive, int page, int pageSize)
    {
        var query = _context.Services.Include(s => s.Images).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.NameEn.Contains(search) || s.NameAr.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);

        if (isActive.HasValue)
            query = query.Where(s => s.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResponse<ServiceDto>.Ok(items.Select(MapToDto), total, page, pageSize);
    }

    public async Task<ApiResponse<ServiceDto>> GetByIdAsync(Guid id)
    {
        var service = await _context.Services
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.id == id);

        if (service == null)
            return ApiResponse<ServiceDto>.NotFound("Service not found");

        return ApiResponse<ServiceDto>.Ok(MapToDto(service));
    }

    public async Task<ApiResponse<ServiceDto>> CreateAsync(CreateServiceDto dto)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.id == dto.CategoryId);
        if (!categoryExists)
            return ApiResponse<ServiceDto>.Fail("Category not found", 404);

        string? imageUrl = null;
        if (dto.Image != null)
            imageUrl = await SaveFileAsync(dto.Image, "services");

        var service = new Service
        {
            id = Guid.NewGuid(),
            CategoryId = dto.CategoryId,
            NameEn = dto.NameEn,
            NameAr = dto.NameAr,
            Description = dto.Description,
            FixedPrice = dto.FixedPrice,
            EstimatedDurationMin = dto.EstimatedDurationMin,
            EstimatedDurationMax = dto.EstimatedDurationMax,
            IsActive = dto.IsActive,
            Image = imageUrl
        };
        _context.Services.Add(service);

        if (dto.ImageUrls?.Any() == true)
        {
            foreach (var file in dto.ImageUrls)
            {
                var url = await SaveFileAsync(file, "services");
                _context.ServiceImages.Add(new ServiceImage { ServiceId = service.id, ImageUrl = url });
            }
        }

        await _context.SaveChangesAsync();

        var created = await _context.Services.Include(s => s.Images).FirstAsync(s => s.id == service.id);
        return ApiResponse<ServiceDto>.Created(MapToDto(created));
    }

    public async Task<ApiResponse<ServiceDto>> UpdateAsync(Guid id, UpdateServiceDto dto)
    {
        var service = await _context.Services.Include(s => s.Images).FirstOrDefaultAsync(s => s.id == id);
        if (service == null)
            return ApiResponse<ServiceDto>.NotFound("Service not found");

        if (dto.NameEn != null) service.NameEn = dto.NameEn;
        if (dto.NameAr != null) service.NameAr = dto.NameAr;
        if (dto.Description != null) service.Description = dto.Description;
        if (dto.FixedPrice.HasValue) service.FixedPrice = dto.FixedPrice.Value;
        if (dto.EstimatedDurationMin.HasValue) service.EstimatedDurationMin = dto.EstimatedDurationMin.Value;
        if (dto.EstimatedDurationMax.HasValue) service.EstimatedDurationMax = dto.EstimatedDurationMax.Value;
        if (dto.IsActive.HasValue) service.IsActive = dto.IsActive.Value;

        if (dto.Image != null)
            service.Image = await SaveFileAsync(dto.Image, "services");

        if (dto.ImageUrls?.Any() == true)
        {
            foreach (var file in dto.ImageUrls)
            {
                var url = await SaveFileAsync(file, "services");
                _context.ServiceImages.Add(new ServiceImage { ServiceId = service.id, ImageUrl = url });
            }
        }

        await _context.SaveChangesAsync();
        return ApiResponse<ServiceDto>.Ok(MapToDto(service));
    }

    public async Task<ApiResponse<string>> ToggleActiveAsync(Guid id)
    {
        var service = await _context.Services.FirstOrDefaultAsync(s => s.id == id);
        if (service == null)
            return ApiResponse<string>.NotFound("Service not found");

        service.IsActive = !service.IsActive;
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok(service.IsActive ? "Service activated" : "Service deactivated");
    }

    public async Task<ApiResponse<List<string>>> GetImagesAsync(Guid serviceId)
    {
        var urls = await _context.ServiceImages
            .Where(i => i.ServiceId == serviceId)
            .Select(i => i.ImageUrl)
            .ToListAsync();

        return ApiResponse<List<string>>.Ok(urls);
    }

    public async Task<ApiResponse<List<string>>> AddImagesAsync(Guid serviceId, List<IFormFile> images)
    {
        var serviceExists = await _context.Services.AnyAsync(s => s.id == serviceId);
        if (!serviceExists)
            return ApiResponse<List<string>>.NotFound("Service not found");

        var urls = new List<string>();
        foreach (var file in images)
        {
            var url = await SaveFileAsync(file, "services");
            _context.ServiceImages.Add(new ServiceImage { ServiceId = serviceId, ImageUrl = url });
            urls.Add(url);
        }

        await _context.SaveChangesAsync();
        return ApiResponse<List<string>>.Ok(urls);
    }

    public async Task<ApiResponse<string>> DeleteImageAsync(Guid imageId)
    {
        var image = await _context.ServiceImages.FirstOrDefaultAsync(i => i.Id == imageId);
        if (image == null)
            return ApiResponse<string>.NotFound("Image not found");

        _context.ServiceImages.Remove(image);
        await _context.SaveChangesAsync();

        return ApiResponse<string>.Ok("Image deleted");
    }

    private async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(uploadsPath);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsPath, fileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{folder}/{fileName}";
    }

    private static ServiceDto MapToDto(Service s) => new()
    {
        Id = s.id,
        CategoryId = s.CategoryId,
        NameEn = s.NameEn,
        NameAr = s.NameAr,
        Description = s.Description,
        Image = s.Image,
        FixedPrice = s.FixedPrice,
        EstimatedDurationMin = s.EstimatedDurationMin,
        EstimatedDurationMax = s.EstimatedDurationMax,
        Rating = s.Rating,
        ReviewCount = s.ReviewCount,
        IsActive = s.IsActive,
        ImageUrls = s.Images?.Select(i => i.ImageUrl).ToList() ?? []
    };
}
