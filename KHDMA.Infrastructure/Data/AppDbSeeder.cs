using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KHDMA.Infrastructure.Data
{
    /// <summary>
    /// Seeds a data set that actually exercises the dispatch engine.
    /// </summary>
    /// <remarks>
    /// The original seeder produced a single provider with no coordinates, State
    /// Pending, AvailabilityStatus Offline and no ProviderService row - every one
    /// of which the eligibility filter rejects, so a dispatch would have found
    /// nobody. This version seeds several ONLINE, APPROVED providers near the
    /// customer, all offering the seeded services, plus two deliberately
    /// ineligible ones (offline, and pending approval) so the filter is visibly
    /// doing its job.
    ///
    /// All coordinates cluster around downtown Cairo (30.0444, 31.2357) inside the
    /// 10 km first dispatch round.
    /// </remarks>
    public static class AppDbSeeder
    {
        private const string Password = "Password123!";
        private const string AdminPassword = "Admin123!";

        // Downtown Cairo - the customer's location.
        private const double CairoLat = 30.0444;
        private const double CairoLng = 31.2357;

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var hasher = new PasswordHasher<ApplicationUser>();

            foreach (var role in new[] { "Customer", "Provider", "Admin" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            await SeedAdminAsync(context, hasher);

            // Idempotent: the demo set is seeded once. Delete the database (or use a
            // fresh Database= name) to re-seed from scratch.
            if (await context.Customers.AnyAsync())
                return;

            var (plumbing, electrical, cleaning) = SeedCategories(context);
            var services = SeedServices(context, plumbing, electrical, cleaning);
            await context.SaveChangesAsync();

            var customer = SeedCustomer(context, hasher);

            // 3 eligible + 2 deliberately ineligible.
            var providerA = SeedProvider(context, hasher, "provider@test.com", "Mohamed Hassan",
                "Professional Plumber", CairoLat + 0.006, CairoLng + 0.004,
                ProviderState.Active, AvailabilityStatus.Online, rating: 4.9, reviews: 87, jobs: 143);

            var providerB = SeedProvider(context, hasher, "provider2@test.com", "Karim Adel",
                "Master Plumber & Electrician", CairoLat - 0.008, CairoLng + 0.010,
                ProviderState.Active, AvailabilityStatus.Online, rating: 4.7, reviews: 52, jobs: 61);

            var providerC = SeedProvider(context, hasher, "provider3@test.com", "Sara Mostafa",
                "Home Cleaning Specialist", CairoLat + 0.012, CairoLng - 0.006,
                ProviderState.Active, AvailabilityStatus.Online, rating: 5.0, reviews: 30, jobs: 40);

            // Filtered out: online but not approved.
            var providerPending = SeedProvider(context, hasher, "provider.pending@test.com", "Omar Waiting",
                "Plumber (awaiting approval)", CairoLat + 0.003, CairoLng + 0.003,
                ProviderState.Pending, AvailabilityStatus.Online, rating: 0, reviews: 0, jobs: 0);

            // Filtered out: approved but offline.
            var providerOffline = SeedProvider(context, hasher, "provider.offline@test.com", "Hana Offline",
                "Plumber (offline)", CairoLat + 0.004, CairoLng + 0.004,
                ProviderState.Active, AvailabilityStatus.Offline, rating: 4.2, reviews: 12, jobs: 15);

            await context.SaveChangesAsync();

            // ---- who offers what ----
            var plumbingServices = services.Where(s => s.CategoryId == plumbing.id).ToList();
            var electricalServices = services.Where(s => s.CategoryId == electrical.id).ToList();
            var cleaningServices = services.Where(s => s.CategoryId == cleaning.id).ToList();

            OfferServices(context, providerA, plumbingServices);
            OfferServices(context, providerB, plumbingServices.Concat(electricalServices));
            OfferServices(context, providerC, cleaningServices);
            OfferServices(context, providerPending, plumbingServices);
            OfferServices(context, providerOffline, plumbingServices);

            // ---- provider portfolio, certificate, service area ----
            providerA.ServiceArea = "Nasr City, Maadi, Heliopolis";
            providerA.Description = "Over 5 years fixing leaks, installations and emergency callouts across Cairo.";
            providerA.ExperienceYears = 5;
            context.ProviderPortfolioImages.Add(new ProviderPortfolioImage
            { ProviderId = providerA.ApplicationUserId, ImageUrl = "/uploads/portfolio/sample1.jpg" });
            context.ProviderCertificateImages.Add(new ProviderCertificateImage
            { ProviderId = providerA.ApplicationUserId, ImageUrl = "/uploads/certificates/sample1.jpg" });

            // ---- a saved address for the customer ----
            context.Addresses.Add(new Address
            {
                UserId = customer.ApplicationUserId,
                Label = "Home",
                AddresssLine = "123 Gardenia St, New Cairo",
                Latitude = CairoLat,
                Longitude = CairoLng,
            });

            await context.SaveChangesAsync();

            // ---- history: two completed jobs for provider A, so earnings/wallet/reviews have data ----
            var pipeRepair = plumbingServices.First();
            SeedCompletedBooking(context, customer, providerA, pipeRepair,
                daysAgo: 6, serviceFee: 250m);
            SeedCompletedBooking(context, customer, providerA, pipeRepair,
                daysAgo: 2, serviceFee: 300m);

            // Reflect that work on the wallet (net of 15% commission).
            providerA.NumberOfJobsDone += 2;
            providerA.TotalEarnings = (250m + 300m) * 0.85m;   // 467.50
            providerA.Balance = (250m + 300m) * 0.85m;

            // ---- one future scheduled booking (unassigned), to exercise the worker ----
            var scheduled = new Booking
            {
                CustomerId = customer.ApplicationUserId,
                ProviderId = null,
                ServiceId = pipeRepair.id,
                BookingType = BookingType.Scheduled,
                ScheduledTime = DateTime.UtcNow.AddHours(3),
                Address = "123 Gardenia St, New Cairo",
                Latitude = CairoLat,
                Longitude = CairoLng,
                Status = BookingStatus.Pending,
                TotalPrice = 275m,
                Notes = "Scheduled - the worker will dispatch this ~30 min before the slot",
            };
            context.Bookings.Add(scheduled);
            context.Payments.Add(BuildPayment(scheduled.Id, 250m, PaymentStatus.Paid));

            await context.SaveChangesAsync();
        }

        // ------------------------------------------------------------------
        // Users
        // ------------------------------------------------------------------

        private static async Task SeedAdminAsync(AppDbContext context, PasswordHasher<ApplicationUser> hasher)
        {
            if (await context.Admins.AnyAsync()) return;

            var admin = NewUser("admin@khdma.com", "System Admin", UserRole.Admin);
            admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);

            context.Users.Add(admin);
            context.Admins.Add(new Admin { ApplicationUserId = admin.Id });
            await context.SaveChangesAsync();
        }

        private static Customer SeedCustomer(AppDbContext context, PasswordHasher<ApplicationUser> hasher)
        {
            var user = NewUser("customer@test.com", "Sara Ahmed", UserRole.Customer);
            user.CreateAt = DateTime.UtcNow.AddDays(-30);
            user.PasswordHash = hasher.HashPassword(user, Password);

            var customer = new Customer { ApplicationUserId = user.Id };
            context.Users.Add(user);
            context.Customers.Add(customer);
            return customer;
        }

        private static Provider SeedProvider(
            AppDbContext context, PasswordHasher<ApplicationUser> hasher,
            string email, string fullName, string jobTitle,
            double lat, double lng, ProviderState state, AvailabilityStatus availability,
            double rating, int reviews, int jobs)
        {
            var user = NewUser(email, fullName, UserRole.Provider);
            user.CreateAt = DateTime.UtcNow.AddDays(-40);
            user.PasswordHash = hasher.HashPassword(user, Password);

            var provider = new Provider
            {
                ApplicationUserId = user.Id,
                State = state,
                AvailabilityStatus = availability,
                CurrentLatitude = lat,
                CurrentLongitude = lng,
                JobTitle = jobTitle,
                HourlyRate = 60m,
                Rating = rating,
                ReviewCount = reviews,
                NumberOfJobsDone = jobs,
            };

            context.Users.Add(user);
            context.Providers.Add(provider);
            return provider;
        }

        private static ApplicationUser NewUser(string email, string fullName, UserRole role) => new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            FullName = fullName,
            Role = role,
            Status = UserStatus.Active,
            PhoneNumber = "+201000000000",
            CreateAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        // ------------------------------------------------------------------
        // Catalogue
        // ------------------------------------------------------------------

        private static (Category Plumbing, Category Electrical, Category Cleaning) SeedCategories(AppDbContext context)
        {
            var plumbing = new Category { NameEn = "Plumbing", NameAr = "سباكة", Description = "Pipes, leaks and installations", IsActive = true };
            var electrical = new Category { NameEn = "Electrical", NameAr = "كهرباء", Description = "Wiring, fixtures and repairs", IsActive = true };
            var cleaning = new Category { NameEn = "Cleaning", NameAr = "تنظيف", Description = "Home and deep cleaning", IsActive = true };

            context.Categories.AddRange(plumbing, electrical, cleaning);
            return (plumbing, electrical, cleaning);
        }

        private static List<Service> SeedServices(AppDbContext context, Category plumbing, Category electrical, Category cleaning)
        {
            var services = new List<Service>
            {
                NewService(plumbing.id, "Pipe Leakage Repair", "إصلاح تسرب المواسير", 250m, 45, 90, 4.9m, 87),
                NewService(plumbing.id, "Drain Unblocking", "تسليك المجاري", 200m, 30, 60, 4.6m, 40),
                NewService(electrical.id, "Wiring Installation", "تركيب أسلاك", 350m, 60, 120, 4.8m, 25),
                NewService(electrical.id, "Fixture Replacement", "استبدال تجهيزات", 180m, 30, 60, 4.5m, 18),
                NewService(cleaning.id, "Deep Home Cleaning", "تنظيف منزلي عميق", 400m, 120, 240, 5.0m, 30),
            };

            context.Services.AddRange(services);
            return services;
        }

        private static Service NewService(
            Guid categoryId, string en, string ar, decimal price, int minDur, int maxDur, decimal rating, int reviews) => new()
        {
            CategoryId = categoryId,
            NameEn = en,
            NameAr = ar,
            Description = $"{en} performed by a vetted professional.",
            FixedPrice = price,
            EstimatedDurationMin = minDur,
            EstimatedDurationMax = maxDur,
            Rating = rating,
            ReviewCount = reviews,
            IsActive = true,
        };

        private static void OfferServices(AppDbContext context, Provider provider, IEnumerable<Service> services)
        {
            foreach (var service in services)
            {
                context.ProviderServices.Add(new ProviderService
                {
                    ProviderId = provider.ApplicationUserId,
                    ServiceId = service.id,
                    IsActive = true,
                });
            }
        }

        // ------------------------------------------------------------------
        // History
        // ------------------------------------------------------------------

        private static void SeedCompletedBooking(
            AppDbContext context, Customer customer, Provider provider, Service service,
            int daysAgo, decimal serviceFee)
        {
            var completedAt = DateTime.UtcNow.AddDays(-daysAgo);

            var booking = new Booking
            {
                CustomerId = customer.ApplicationUserId,
                ProviderId = provider.ApplicationUserId,
                ServiceId = service.id,
                BookingType = BookingType.Immediate,
                Address = "123 Gardenia St, New Cairo",
                Latitude = CairoLat,
                Longitude = CairoLng,
                Status = BookingStatus.Completed,
                TotalPrice = serviceFee * 1.10m,
                CreateAt = completedAt.AddHours(-2),
                AcceptedAt = completedAt.AddHours(-2).AddMinutes(1),
                EnRouteAt = completedAt.AddHours(-2).AddMinutes(5),
                ArrivedAt = completedAt.AddHours(-1),
                StartedAt = completedAt.AddHours(-1).AddMinutes(5),
                CompletedAt = completedAt,
            };
            context.Bookings.Add(booking);

            context.Payments.Add(BuildPayment(booking.Id, serviceFee, PaymentStatus.Paid, completedAt));

            // A trail of transitions, so the admin dashboard's dispatch-to-accept
            // metric and GET /booking/history have something to show.
            foreach (var (from, to, at) in new[]
            {
                (BookingStatus.Pending, BookingStatus.Dispatching, booking.CreateAt),
                (BookingStatus.Dispatching, BookingStatus.Accepted, booking.AcceptedAt!.Value),
                (BookingStatus.Accepted, BookingStatus.EnRoute, booking.EnRouteAt!.Value),
                (BookingStatus.EnRoute, BookingStatus.Arrived, booking.ArrivedAt!.Value),
                (BookingStatus.Arrived, BookingStatus.InProgress, booking.StartedAt!.Value),
                (BookingStatus.InProgress, BookingStatus.Completed, booking.CompletedAt!.Value),
            })
            {
                context.BookingStatusHistories.Add(new BookingStatusHistory
                {
                    BookingId = booking.Id,
                    FromStatus = from,
                    ToStatus = to,
                    ChangedAt = at,
                    ChangedByUserId = provider.ApplicationUserId,
                });
            }

            context.Reviews.Add(new Review
            {
                BookingId = booking.Id,
                CustomerId = customer.ApplicationUserId,
                ProviderId = provider.ApplicationUserId,
                Rating = 5,
                Comment = "Fixed my kitchen leak in no time. Very professional.",
                PunctualityRating = 5,
                WorkQualityRating = 5,
                CleanlinesRating = 4,
                CreateAt = completedAt.AddMinutes(30),
            });
        }

        private static Payment BuildPayment(Guid bookingId, decimal serviceFee, PaymentStatus status, DateTime? paidAt = null)
        {
            var vat = decimal.Round(serviceFee * 0.10m, 2);
            var commission = decimal.Round(serviceFee * 0.15m, 2);

            return new Payment
            {
                BookingId = bookingId,
                Amount = serviceFee + vat,
                ServiceFee = serviceFee,
                VatAmount = vat,
                CommissionAmount = commission,
                ProviderEarning = serviceFee - commission,
                PaymentStatus = status,
                TransactionReference = status == PaymentStatus.Paid ? $"seed_{Guid.NewGuid():N}" : null,
                PaidAt = status == PaymentStatus.Paid ? (paidAt ?? DateTime.UtcNow) : null,
            };
        }
    }
}
