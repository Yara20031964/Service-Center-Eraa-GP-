using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KHDMA.Domain.Entities;

namespace KHDMA.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ProviderService> ProviderServices { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<CommissionSettings> CommissionSettings { get; set; }
        public DbSet<ServiceImage> ServiceImages { get; set; }
        public DbSet<CustomerFavorite> CustomerFavorites { get; set; }
        public DbSet<CustomerFavoriteProvider> CustomerFavoriteProviders { get; set; }
        public DbSet<ProviderPortfolioImage> ProviderPortfolioImages { get; set; }
        public DbSet<ProviderCertificateImage> ProviderCertificateImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer - 1:1 with ApplicationUser
            modelBuilder.Entity<Customer>()
                .HasKey(c => c.ApplicationUserId);
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.ApplicationUser)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(c => c.ApplicationUserId);

            // Provider - 1:1 with ApplicationUser
            modelBuilder.Entity<Provider>()
                .HasKey(p => p.ApplicationUserId);
            modelBuilder.Entity<Provider>()
                .HasOne(p => p.ApplicationUser)
                .WithOne(u => u.Provider)
                .HasForeignKey<Provider>(p => p.ApplicationUserId);

            // Admin - 1:1 with ApplicationUser
            modelBuilder.Entity<Admin>()
                .HasKey(a => a.ApplicationUserId);
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.ApplicationUser)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.ApplicationUserId);

            // Category → Service 1:N
            modelBuilder.Entity<Service>()
                .HasOne(s => s.Category)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.CategoryId);

            // Provider → ProviderService 1:N
            modelBuilder.Entity<ProviderService>()
                .HasOne(ps => ps.Provider)
                .WithMany(p => p.ProviderServices)
                .HasForeignKey(ps => ps.ProviderId);

            // Service → ProviderService 1:N
            modelBuilder.Entity<ProviderService>()
                .HasOne(ps => ps.Service)
                .WithMany(s => s.ProviderServices)
                .HasForeignKey(ps => ps.ServiceId);

            // Customer → Booking 1:N
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany(c => c.Bookings)
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Provider → Booking 1:N
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Provider)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Service → Booking 1:N
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.ServiceId);

            // Booking → Payment 1:1
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → Review 1:1
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Booking)
                .WithOne(b => b.Review)
                .HasForeignKey<Review>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review → Customer
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review → Provider
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Provider)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → ChatMessage 1:N
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.Booking)
                .WithMany(b => b.ChatMessages)
                .HasForeignKey(cm => cm.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Service → ServiceImage 1:N
            modelBuilder.Entity<ServiceImage>()
                .HasOne(si => si.Service)
                .WithMany(s => s.Images)
                .HasForeignKey(si => si.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // CustomerFavorite - composite PK (many-to-many: Customer ↔ Service)
            modelBuilder.Entity<CustomerFavorite>()
                .HasKey(cf => new { cf.CustomerId, cf.ServiceId });
            modelBuilder.Entity<CustomerFavorite>()
                .HasOne(cf => cf.Customer)
                .WithMany(c => c.Favorites)
                .HasForeignKey(cf => cf.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CustomerFavorite>()
                .HasOne(cf => cf.Service)
                .WithMany(s => s.Favorites)
                .HasForeignKey(cf => cf.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser → Address 1:N
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId);

            // CustomerFavoriteProvider - composite PK (many-to-many: Customer ↔ Provider)
            modelBuilder.Entity<CustomerFavoriteProvider>()
                .HasKey(cf => new { cf.CustomerId, cf.ProviderId });
            modelBuilder.Entity<CustomerFavoriteProvider>()
                .HasOne(cf => cf.Customer)
                .WithMany(c => c.FavoriteProviders)
                .HasForeignKey(cf => cf.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CustomerFavoriteProvider>()
                .HasOne(cf => cf.Provider)
                .WithMany(p => p.FavoritedBy)
                .HasForeignKey(cf => cf.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Provider → ProviderPortfolioImage 1:N
            modelBuilder.Entity<ProviderPortfolioImage>()
                .HasOne(pi => pi.Provider)
                .WithMany(p => p.PortfolioImages)
                .HasForeignKey(pi => pi.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Provider → ProviderCertificateImage 1:N
            modelBuilder.Entity<ProviderCertificateImage>()
                .HasOne(ci => ci.Provider)
                .WithMany(p => p.CertificateImages)
                .HasForeignKey(ci => ci.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser → Notification 1:N
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId);
            modelBuilder.Entity<CommissionSettings>().HasData(
                new CommissionSettings
                {
                    Id = 1,
                    Rate = 0.15m,
                    LastUpdatedAt = new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedBy = "system"
                }
            );

        }
    }
}