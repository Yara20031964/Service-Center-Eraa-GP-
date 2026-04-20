using Application.Services.Admin;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Infrastructure.Data;
using KHDMA.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAdminCustomerService, AdminCustomerService>();
builder.Services.AddScoped<IAdminProviderService, AdminProviderService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<KHDMA.Application.Interfaces.Services.Admin.IAdminBookingService, KHDMA.Infrastructure.Services.Admin.AdminBookingService>();
builder.Services.AddScoped<KHDMA.Application.Interfaces.Services.Admin.IAdminPaymentService, KHDMA.Infrastructure.Services.Admin.AdminPaymentService>();
builder.Services.AddScoped<KHDMA.Application.Interfaces.Services.Admin.IAdminReviewService, KHDMA.Infrastructure.Services.Admin.AdminReviewService>();
builder.Services.AddScoped<KHDMA.Application.Interfaces.Services.IStripePaymentService, KHDMA.Infrastructure.Services.Payment.StripePaymentService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Apply pending migrations and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
await KHDMA.Infrastructure.Data.AppDbSeeder.SeedAsync(app.Services);

app.Run();