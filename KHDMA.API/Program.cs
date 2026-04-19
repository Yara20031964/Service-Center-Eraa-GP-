using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;
using KHDMA.Infrastructure.Data;
using KHDMA.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
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

// Run Database Seeder
await KHDMA.Infrastructure.Data.AppDbSeeder.SeedAsync(app.Services);

app.Run();