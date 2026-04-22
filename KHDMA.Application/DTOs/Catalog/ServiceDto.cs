using Microsoft.AspNetCore.Http;

namespace KHDMA.Application.DTOs.Catalog;

public class ServiceDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public decimal? FixedPrice { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public int? EstimatedDurationMax { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsActive { get; set; }
    public List<string> ImageUrls { get; set; } = [];
}

public class CreateServiceDto
{
    public Guid CategoryId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? FixedPrice { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public int? EstimatedDurationMax { get; set; }
    public bool IsActive { get; set; } = true;
    public IFormFile? Image { get; set; }
    public List<IFormFile>? ImageUrls { get; set; }
}

public class UpdateServiceDto
{
    public string? NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public decimal? FixedPrice { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public int? EstimatedDurationMax { get; set; }
    public bool? IsActive { get; set; }
    public IFormFile? Image { get; set; }
    public List<IFormFile>? ImageUrls { get; set; }
}
