using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using KHDMA.Domain.Entities;
using KHDMA.Domain.Enums;

namespace KHDMA.Infrastructure.Data
{
    public static class AppDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await context.Users.AnyAsync())
            {
                return; // DB has been seeded
            }

            var hasher = new PasswordHasher<ApplicationUser>();

            // 1. Create Users
            var customerUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "customer@test.com",
                NormalizedUserName = "CUSTOMER@TEST.COM",
                Email = "customer@test.com",
                NormalizedEmail = "CUSTOMER@TEST.COM",
                EmailConfirmed = true,
                FullName = "Ahmed Customer",
                Role = UserRole.Customer,
                Status = UserStatus.Active,
                CreateAt = DateTime.UtcNow.AddDays(-30)
            };
            customerUser.PasswordHash = hasher.HashPassword(customerUser, "Password123!");

            var providerUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "provider@test.com",
                NormalizedUserName = "PROVIDER@TEST.COM",
                Email = "provider@test.com",
                NormalizedEmail = "PROVIDER@TEST.COM",
                EmailConfirmed = true,
                FullName = "Eraa Provider",
                Role = UserRole.Provider,
                Status = UserStatus.Active,
                CreateAt = DateTime.UtcNow.AddDays(-40)
            };
            providerUser.PasswordHash = hasher.HashPassword(providerUser, "Password123!");

            context.Users.AddRange(customerUser, providerUser);

            // 2. Create Roles Instances
            var customer = new Customer
            {
                ApplicationUserId = customerUser.Id
            };
            var provider = new Provider
            {
                ApplicationUserId = providerUser.Id,
                HourlyRate = 50.0m
            };

            context.Customers.Add(customer);
            context.Providers.Add(provider);

            // 3. Create Category & Service
            var category = new Category
            {
                id = Guid.NewGuid(),
                NameEn = "Plumbing",
                NameAr = "سباكة",
                Description = "Fix pipes and water leaks"
            };
            context.Categories.Add(category);

            var service = new Service
            {
                id = Guid.NewGuid(),
                NameEn = "Pipe Repair",
                NameAr = "تصليح الأنابيب",
                FixedPrice = 150.0m,
                CategoryId = category.id
            };
            context.Services.Add(service);

            // 4. Create Bookings
            var booking1 = new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customerUser.Id,
                ProviderId = providerUser.Id,
                ServiceId = service.id,
                BookingType = BookingType.Scheduled,
                ScheduledTime = DateTime.UtcNow.AddDays(-10),
                Address = "123 Main St",
                Latitude = 30.0444,
                Longitude = 31.2357,
                Status = BookingStatus.Accepted,
                TotalPrice = 150.0m,
                Notes = "Please bring extra pipes",
                CreateAt = DateTime.UtcNow.AddDays(-15)
            };

            var booking2 = new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customerUser.Id,
                ProviderId = providerUser.Id,
                ServiceId = service.id,
                BookingType = BookingType.Immediate,
                ScheduledTime = DateTime.UtcNow.AddDays(-5),
                Address = "456 Side St",
                Status = BookingStatus.Completed,
                TotalPrice = 200.0m,
                CreateAt = DateTime.UtcNow.AddDays(-6)
            };
            var booking3 = new Booking
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CustomerId = customerUser.Id,
                ProviderId = providerUser.Id,
                ServiceId = service.id,
                BookingType = BookingType.Immediate,
                ScheduledTime = DateTime.UtcNow.AddHours(2),
                Address = "789 Test St",
                Status = BookingStatus.Pending,
                TotalPrice = 100.0m,
                Notes = "Need this immediately! (Unpaid)",
                CreateAt = DateTime.UtcNow
            };
            
            context.Bookings.AddRange(booking1, booking2, booking3);

            // 5. Create Payments (Assuming Booking 2 is completed & paid, Booking 1 is confirmed)
            var payment1 = new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking1.Id,
                Amount = 150.0m,
                CommissionAmount = 15.0m,
                ProviderEarning = 135.0m,
                PaymentStatus = PaymentStatus.Paid,
                TransactionReference = "pi_test_123456789",
                PaidAt = DateTime.UtcNow.AddDays(-14)
            };

            var payment2 = new Payment
            {
                Id = Guid.NewGuid(),
                BookingId = booking2.Id,
                Amount = 200.0m,
                CommissionAmount = 20.0m,
                ProviderEarning = 180.0m,
                PaymentStatus = PaymentStatus.Paid,
                TransactionReference = "pi_test_987654321",
                PaidAt = DateTime.UtcNow.AddDays(-5)
            };

            context.Payments.AddRange(payment1, payment2);

            // 6. Create Review
            var review = new Review
            {
                Id = Guid.NewGuid(),
                BookingId = booking2.Id,
                CustomerId = customerUser.Id,
                ProviderId = providerUser.Id,
                Rating = 5,
                Comment = "Excellent and fast work!",
                PunctualityRating = 5,
                WorkQualityRating = 5,
                CleanlinesRating = 4,
                CreateAt = DateTime.UtcNow.AddDays(-4),
                IsHidden = false,
                IsDeleted = false
            };

            context.Reviews.Add(review);

            // Save all
            await context.SaveChangesAsync();
        }
    }
}
