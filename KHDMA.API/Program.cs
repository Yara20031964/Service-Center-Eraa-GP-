using Application.Interfaces.Services;
using Application.Services.Admin;
using KHDMA.Application.Interfaces.Payment;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Entities;
using KHDMA.Infrastructure.Data;
using KHDMA.Infrastructure.Repositories;
using KHDMA.Infrastructure.Services;
using KHDMA.Infrastructure.Services.Admin;
using KHDMA.Application.Interfaces;
using KHDMA.Infrastructure.Services.Payment;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/khdma-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontends", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://your-admin-domain.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAdminModerationService, AdminModerationService>();
builder.Services.AddScoped<IAdminCustomerService, AdminCustomerService>();
builder.Services.AddScoped<IAdminProviderService, AdminProviderService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<IAdminBookingService, AdminBookingService>();
builder.Services.AddScoped<IAdminPaymentService, AdminPaymentService>();
builder.Services.AddScoped<IAdminReviewService, AdminReviewService>();
builder.Services.AddScoped<KHDMA.Application.Interfaces.Services.IStripePaymentService,
    KHDMA.Infrastructure.Services.Payment.StripePaymentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
builder.Services.AddScoped<IAdminServiceService, AdminServiceService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IDispatchService, DispatchService>();
builder.Services.AddScoped<ICancellationPolicy, CancellationPolicy>();
builder.Services.AddScoped<IExportService, ExportService>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(KHDMA.Application.Interfaces.IBookingService).Assembly));
builder.Services.AddScoped<IAdminFinanceService, AdminFinanceService>();
builder.Services.AddHttpClient<IPaymobService, PaymobService>();

builder.Services.AddScoped<IAdminContentService, AdminContentService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KHDMA API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseCors("AllowFrontends");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<KHDMA.API.Middleware.AuditMiddleware>();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
await KHDMA.Infrastructure.Data.AppDbSeeder.SeedAsync(app.Services);

app.Run();