using System.Linq.Expressions;
using Domain.Common;
using KHDMA.Application.DTOs.Catalog;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using KHDMA.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KHDMA.Infrastructure.Services
{
    public class PublicCatalogService : IPublicCatalogService
    {
        /// <summary>
        /// Shared service projection.
        /// </summary>
        /// <remarks>
        /// An <see cref="Expression"/> field rather than a method: EF Core cannot
        /// translate a call to a method with a body, so a helper method here would
        /// either throw at runtime or pull every row into memory before mapping.
        /// As an expression tree it composes into the SQL.
        /// </remarks>
        private static readonly Expression<Func<Service, PublicServiceDto>> ServiceProjection =
            s => new PublicServiceDto
            {
                Id = s.id,
                NameEn = s.NameEn,
                NameAr = s.NameAr,
                DescriptionEn = s.Description,
                DescriptionAr = s.Description,
                ImageUrl = s.Image,
                ImageUrls = s.Images.Select(i => i.ImageUrl).ToList(),
                CategoryId = s.CategoryId,
                CategoryNameEn = s.Category.NameEn,
                CategoryNameAr = s.Category.NameAr,
                FixedPrice = s.FixedPrice,
                EstimatedDurationMin = s.EstimatedDurationMin,
                EstimatedDurationMax = s.EstimatedDurationMax,
                Rating = s.Rating,
                ReviewCount = s.ReviewCount,
            };

        private readonly AppDbContext _db;
        private readonly IPricingService _pricing;
        private readonly IPresenceStore _presence;
        private readonly IImageUrlResolver _imageUrlResolver;

        public PublicCatalogService(
            AppDbContext db,
            IPricingService pricing,
            IPresenceStore presence,
            IImageUrlResolver imageUrlResolver)
        {
            _db = db;
            _pricing = pricing;
            _presence = presence;
            _imageUrlResolver = imageUrlResolver;
        }

        public async Task<ApiResponse<List<PublicCategoryDto>>> GetCategoriesAsync()
        {
            var categories = await _db.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.NameEn)
                .Select(c => new PublicCategoryDto
                {
                    Id = c.id,
                    NameEn = c.NameEn,
                    NameAr = c.NameAr,
                    IconUrl = c.IconUrl,
                    ServiceCount = c.Services.Count(s => s.IsActive),
                })
                .ToListAsync();

            foreach (var category in categories)
                category.IconUrl = _imageUrlResolver.Resolve(category.IconUrl);

            return ApiResponse<List<PublicCategoryDto>>.Ok(categories);
        }

        public async Task<PagedResponse<PublicServiceDto>> GetServicesAsync(
            Guid? categoryId, string? search, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 50) pageSize = 10;

            var query = _db.Services.AsNoTracking().Where(s => s.IsActive && s.Category.IsActive);

            if (categoryId is not null)
                query = query.Where(s => s.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(s => s.NameEn.Contains(term) || s.NameAr.Contains(term));
            }

            var total = await query.CountAsync();

            // Skip/Take in SQL. GenericRepository.GetAsync pages in memory, which is
            // why this talks to the DbContext directly.
            var items = await query
                .OrderByDescending(s => s.Rating)
                .ThenBy(s => s.NameEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ServiceProjection)
                .ToListAsync();

            foreach (var item in items)
                ResolveServiceImageUrls(item);

            return PagedResponse<PublicServiceDto>.Ok(items, total, page, pageSize);
        }

        public async Task<ApiResponse<PublicServiceDetailDto>> GetServiceByIdAsync(Guid id)
        {
            var basics = await _db.Services
                .AsNoTracking()
                .Where(s => s.id == id && s.IsActive)
                .Select(ServiceProjection)
                .FirstOrDefaultAsync();

            if (basics is null) return ApiResponse<PublicServiceDetailDto>.NotFound("Service not found");
            ResolveServiceImageUrls(basics);

            var providerCount = await _db.ProviderServices
                .AsNoTracking()
                .CountAsync(ps => ps.ServiceId == id && ps.IsActive
                               && ps.Provider.State == ProviderState.Active);

            var detail = new PublicServiceDetailDto
            {
                Id = basics.Id,
                NameEn = basics.NameEn,
                NameAr = basics.NameAr,
                DescriptionEn = basics.DescriptionEn,
                DescriptionAr = basics.DescriptionAr,
                ImageUrl = basics.ImageUrl,
                ImageUrls = basics.ImageUrls,
                CategoryId = basics.CategoryId,
                CategoryNameEn = basics.CategoryNameEn,
                CategoryNameAr = basics.CategoryNameAr,
                FixedPrice = basics.FixedPrice,
                EstimatedDurationMin = basics.EstimatedDurationMin,
                EstimatedDurationMax = basics.EstimatedDurationMax,
                Rating = basics.Rating,
                ReviewCount = basics.ReviewCount,
                AvailableProvidersCount = providerCount,
            };

            // The same calculation the booking will use, so the checkout screen and
            // the eventual charge cannot disagree.
            var price = await _pricing.ForServiceAsync(id);
            if (price is not null)
            {
                detail.VatRate = price.VatRate;
                detail.VatAmount = price.VatAmount;
                detail.Total = price.Total;
                detail.Currency = price.Currency;
            }

            return ApiResponse<PublicServiceDetailDto>.Ok(detail);
        }

        public async Task<PagedResponse<PublicProviderCardDto>> GetProvidersAsync(
            Guid? category,
            string? search,
            double? lat,
            double? lng,
            double? radiusKm,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 50) pageSize = 10;

            var nearby = lat.HasValue && lng.HasValue;
            var effectiveRadiusKm = radiusKm is > 0 ? radiusKm.Value : 25d;

            var query = _db.Providers
                .AsNoTracking()
                .Where(p => p.State == ProviderState.Active && !p.ApplicationUser.IsDeleted);

            if (category.HasValue)
            {
                query = query.Where(p => p.ProviderServices.Any(ps =>
                    ps.IsActive
                    && ps.Service.IsActive
                    && ps.Service.CategoryId == category.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p =>
                    p.ApplicationUser.FullName.Contains(term)
                    || (p.JobTitle != null && p.JobTitle.Contains(term)));
            }

            if (nearby)
            {
                var latitudeDelta = effectiveRadiusKm / 111.0;
                var longitudeScale = Math.Max(
                    0.01,
                    Math.Abs(Math.Cos(lat!.Value * Math.PI / 180.0)));
                var longitudeDelta = effectiveRadiusKm / (111.32 * longitudeScale);

                // Working point, matching dispatch: "providers near me" should list
                // who can actually be sent here, not who happens to be driving past.
                query = query
                    .Where(p => p.AvailabilityStatus == AvailabilityStatus.Online)
                    .Where(p => p.WorkingLatitude != null && p.WorkingLongitude != null)
                    .Where(p => p.WorkingLatitude >= lat.Value - latitudeDelta
                             && p.WorkingLatitude <= lat.Value + latitudeDelta)
                    .Where(p => p.WorkingLongitude >= lng!.Value - longitudeDelta
                             && p.WorkingLongitude <= lng.Value + longitudeDelta);
            }

            var rows = await query
                .Select(p => new
                {
                    Id = p.ApplicationUserId,
                    Name = p.ApplicationUser.FullName,
                    Photo = p.ApplicationUser.ProfilePictureUrl,
                    p.JobTitle,
                    p.Rating,
                    p.ReviewCount,
                    p.HourlyRate,
                    p.WorkingLatitude,
                    p.WorkingLongitude,
                })
                .ToListAsync();

            var cards = rows
                .AsEnumerable()
                .Select(p => new PublicProviderCardDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Photo = _imageUrlResolver.Resolve(p.Photo),
                    JobTitle = p.JobTitle,
                    Rating = p.Rating,
                    ReviewCount = p.ReviewCount,
                    HourlyRate = p.HourlyRate,
                    DistanceKm = nearby
                        ? Math.Round(DispatchService.Haversine(
                            lat!.Value,
                            lng!.Value,
                            p.WorkingLatitude!.Value,
                            p.WorkingLongitude!.Value), 2)
                        : null,
                });

            cards = nearby
                ? cards.Where(p => p.DistanceKm <= effectiveRadiusKm)
                    .OrderBy(p => p.DistanceKm)
                    .ThenByDescending(p => p.Rating)
                    .ThenBy(p => p.Name)
                : cards.OrderByDescending(p => p.Rating)
                    .ThenBy(p => p.Name);

            var materialized = cards.ToList();
            var total = materialized.Count;
            var items = materialized
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return PagedResponse<PublicProviderCardDto>.Ok(items, total, page, pageSize);
        }

        public async Task<ApiResponse<ProviderPublicProfileDto>> GetProviderProfileAsync(string providerId)
        {
            var provider = await _db.Providers
                .AsNoTracking()
                .Where(p => p.ApplicationUserId == providerId
                         && p.State == ProviderState.Active
                         && !p.ApplicationUser.IsDeleted)
                .Select(p => new
                {
                    p.ApplicationUserId,
                    p.ApplicationUser.FullName,
                    p.ApplicationUser.ProfilePictureUrl,
                    p.JobTitle,
                    p.Rating,
                    p.ReviewCount,
                    p.NumberOfJobsDone,
                    p.ExperienceYears,
                    p.Description,
                    p.ServiceArea,
                    p.AvailabilityStatus,

                    // Written out rather than reusing ServiceProjection: an
                    // expression field cannot be invoked inside another projection.
                    Services = p.ProviderServices
                        .Where(ps => ps.IsActive && ps.Service.IsActive)
                        .Select(ps => new PublicServiceDto
                        {
                            Id = ps.Service.id,
                            NameEn = ps.Service.NameEn,
                            NameAr = ps.Service.NameAr,
                            DescriptionEn = ps.Service.Description,
                            DescriptionAr = ps.Service.Description,
                            ImageUrl = ps.Service.Image,
                            CategoryId = ps.Service.CategoryId,
                            CategoryNameEn = ps.Service.Category.NameEn,
                            CategoryNameAr = ps.Service.Category.NameAr,
                            FixedPrice = ps.Service.FixedPrice,
                            EstimatedDurationMin = ps.Service.EstimatedDurationMin,
                            EstimatedDurationMax = ps.Service.EstimatedDurationMax,
                            Rating = ps.Service.Rating,
                            ReviewCount = ps.Service.ReviewCount,
                        })
                        .ToList(),

                    Portfolio = p.PortfolioImages.Select(i => i.ImageUrl).ToList(),

                    Certificates = p.CertificateImages
                        .Select(c => new ProviderCertificateDto { Id = c.Id, ImageUrl = c.ImageUrl })
                        .ToList(),

                    Reviews = p.Reviews
                        .Where(r => !r.IsHidden && !r.IsDeleted)
                        .OrderByDescending(r => r.CreateAt)
                        .Take(10)
                        .Select(r => new ProviderReviewDto
                        {
                            Id = r.Id,
                            CustomerName = r.Customer.ApplicationUser.FullName,
                            CustomerAvatarUrl = r.Customer.ApplicationUser.ProfilePictureUrl,
                            Rating = r.Rating,
                            Comment = r.Comment,
                            CreatedAt = r.CreateAt,
                        })
                        .ToList(),
                })
                .FirstOrDefaultAsync();

            if (provider is null)
                return ApiResponse<ProviderPublicProfileDto>.NotFound("Provider not found");

            var isConnected = await _presence.IsOnlineAsync(providerId);
            foreach (var service in provider.Services)
                ResolveServiceImageUrls(service);
            foreach (var certificate in provider.Certificates)
                certificate.ImageUrl = _imageUrlResolver.Resolve(certificate.ImageUrl)!;
            foreach (var review in provider.Reviews)
                review.CustomerAvatarUrl = _imageUrlResolver.Resolve(review.CustomerAvatarUrl);

            return ApiResponse<ProviderPublicProfileDto>.Ok(new ProviderPublicProfileDto
            {
                Id = provider.ApplicationUserId,
                FullName = provider.FullName,
                JobTitle = provider.JobTitle,
                AvatarUrl = _imageUrlResolver.Resolve(provider.ProfilePictureUrl),
                IsVerified = true,   // only Active providers reach here
                IsOnline = provider.AvailabilityStatus == AvailabilityStatus.Online && isConnected,
                // SRS 8: withhold the headline figure until it means something.
                Rating = provider.ReviewCount >= 3 ? provider.Rating : null,
                ReviewCount = provider.ReviewCount,
                NumberOfJobsDone = provider.NumberOfJobsDone,
                ExperienceYears = provider.ExperienceYears,
                DescriptionEn = provider.Description,
                DescriptionAr = provider.Description,
                ServicesOffered = provider.Services,
                WorkingAreas = SplitServiceAreas(provider.ServiceArea),
                PortfolioImages = provider.Portfolio
                    .Select(url => _imageUrlResolver.Resolve(url)!)
                    .ToList(),
                Certificates = provider.Certificates,
                Reviews = provider.Reviews,
            });
        }

        private void ResolveServiceImageUrls(PublicServiceDto service)
        {
            service.ImageUrl = _imageUrlResolver.Resolve(service.ImageUrl);
            service.ImageUrls = service.ImageUrls
                .Select(url => _imageUrlResolver.Resolve(url)!)
                .ToList();
        }

        /// <summary>
        /// <c>Provider.ServiceArea</c> is a single free-text column, but the profile
        /// renders one chip per area - so it is split on the usual separators.
        /// A dedicated table should replace this.
        /// </summary>
        private static List<string> SplitServiceAreas(string? serviceArea)
        {
            if (string.IsNullOrWhiteSpace(serviceArea)) return [];

            return serviceArea
                .Split([',', ';', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
