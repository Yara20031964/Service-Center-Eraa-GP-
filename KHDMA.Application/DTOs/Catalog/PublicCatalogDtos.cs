namespace KHDMA.Application.DTOs.Catalog
{
    /// <summary>Category tile on the home screen.</summary>
    public class PublicCategoryDto
    {
        public Guid Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public int ServiceCount { get; set; }
    }

    /// <summary>
    /// A bookable service, as shown to an anonymous browser.
    /// Mirrors the internal ServiceDto minus <c>isActive</c> - the endpoint only
    /// ever returns active services.
    /// </summary>
    public class PublicServiceDto
    {
        public Guid Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> ImageUrls { get; set; } = [];
        public Guid CategoryId { get; set; }
        public string CategoryNameEn { get; set; } = string.Empty;
        public string CategoryNameAr { get; set; } = string.Empty;
        public decimal? FixedPrice { get; set; }
        public string Currency { get; set; } = "EGP";
        public int? EstimatedDurationMin { get; set; }
        public int? EstimatedDurationMax { get; set; }
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
    }

    /// <summary>A compact provider tile for public browse, search, and map screens.</summary>
    public class PublicProviderCardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Photo { get; set; }
        public string? JobTitle { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public decimal? HourlyRate { get; set; }
        public double? DistanceKm { get; set; }
    }

    /// <summary>Service detail page. Adds the price breakdown the checkout screen needs.</summary>
    public class PublicServiceDetailDto : PublicServiceDto
    {
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }

        /// <summary>
        /// Backs the three "what's included" bullets.
        /// </summary>
        /// <remarks>
        /// Always empty for now - <c>Service</c> has no inclusions column. Either a
        /// ServiceInclusion table follows or the UI section is dropped
        /// (docs/API_CONTRACTS.md open question 3).
        /// </remarks>
        public List<ServiceInclusionDto> WhatsIncluded { get; set; } = [];

        public int AvailableProvidersCount { get; set; }
    }

    public class ServiceInclusionDto
    {
        public string TextEn { get; set; } = string.Empty;
        public string TextAr { get; set; } = string.Empty;
    }

    /// <summary>A provider's public profile page.</summary>
    public class ProviderPublicProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? JobTitle { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool IsOnline { get; set; }

        /// <summary>Null until the provider has at least 3 reviews (SRS 8).</summary>
        public double? Rating { get; set; }
        public int ReviewCount { get; set; }
        public int NumberOfJobsDone { get; set; }
        public int? ExperienceYears { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }

        public List<PublicServiceDto> ServicesOffered { get; set; } = [];

        /// <summary>
        /// Split from the single <c>Provider.ServiceArea</c> string so the UI can
        /// render one chip per area. A proper table should replace this.
        /// </summary>
        public List<string> WorkingAreas { get; set; } = [];

        public List<string> PortfolioImages { get; set; } = [];
        public List<ProviderCertificateDto> Certificates { get; set; } = [];
        public List<ProviderReviewDto> Reviews { get; set; } = [];
    }

    public class ProviderCertificateDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Null: <c>ProviderCertificateImage</c> stores only a url. Add the columns
        /// or drop the labels (docs/API_CONTRACTS.md open question 2).
        /// </summary>
        public string? Name { get; set; }
        public string? Issuer { get; set; }
    }

    public class ProviderReviewDto
    {
        public Guid Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerAvatarUrl { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
