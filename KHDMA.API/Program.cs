using Application.Interfaces.Services;
using Application.Services.Admin;
using KHDMA.API.Hubs;
using KHDMA.Application.Interfaces.Payment;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Application.Interfaces.Services;
using KHDMA.Application.Interfaces.Services.Admin;
using KHDMA.Domain.Entities;
using KHDMA.Infrastructure;
using KHDMA.Infrastructure.Data;
using KHDMA.Infrastructure.Repositories;
using KHDMA.Infrastructure.Services;
using KHDMA.Infrastructure.Services.Admin;
using KHDMA.Application.Common;
using KHDMA.Application.Interfaces;
using KHDMA.Application.Interfaces.RealTime;
using KHDMA.Infrastructure.BackgroundJobs;
using KHDMA.Infrastructure.RealTime;
using KHDMA.Infrastructure.Services.Payment;
using StackExchange.Redis;
using Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql =>
        {
            // The database is a remote (free-tier) host that is slow to wake and
            // occasionally drops the first connection. Retry transient failures
            // instead of crashing startup - this is what the SqlException message
            // ("consider enabling retry resiliency") is asking for. Safe here: the
            // codebase uses no explicit BeginTransaction, which retry cannot span.
            sql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);

            // Applying migrations to a remote DB can exceed the 30s default.
            sql.CommandTimeout(120);
        }));

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

    // A browser cannot set an Authorization header on a WebSocket handshake, so
    // SignalR clients pass the token as ?access_token=. The /hubs path guard
    // matters: without it, tokens would be accepted from the query string on
    // every endpoint, where they leak into access logs and Referer headers.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },

        // A rejected request would otherwise come back as a bodiless 401/403, the
        // one response a client cannot parse as ApiResponse. Write the envelope
        // ourselves so "every response has the same shape" holds without exception.
        OnChallenge = async context =>
        {
            context.HandleResponse();
            if (context.Response.HasStarted) return;

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                Domain.Common.ApiResponse<object>.Unauthorized(
                    string.IsNullOrEmpty(context.ErrorDescription)
                        ? "Unauthorized - a valid Bearer token is required"
                        : context.ErrorDescription));
        },

        OnForbidden = async context =>
        {
            if (context.Response.HasStarted) return;

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(
                Domain.Common.ApiResponse<object>.Forbidden(
                    "Forbidden - your role does not have access to this endpoint"));
        },
    };
});
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>  {
    var supportedCultures= new[] { "en", "ar" };
    options.SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
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
builder.Services.AddScoped<IEarningsService, EarningsService>();
builder.Services.AddScoped<KHDMA.Application.Interfaces.Services.IStripePaymentService,
    KHDMA.Infrastructure.Services.Payment.StripePaymentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IImageUrlResolver, ImageUrlResolver>();
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
builder.Services.AddScoped<IAdminServiceService, AdminServiceService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// Local disk rather than BlobStorageService: every method on that class still
// throws NotImplementedException, so with it registered any upload is a 500.
// Swap this line back once Azure Blob is actually configured and implemented.
builder.Services.AddScoped<IBlobStorageService, LocalFileStorageService>();

builder.Services.AddScoped<IDispatchService, DispatchService>();
builder.Services.AddScoped<ICancellationPolicy, CancellationPolicyService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IBookingAccessService, BookingAccessService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IPublicCatalogService, PublicCatalogService>();
builder.Services.AddScoped<IBookingDetailsService, BookingDetailsService>();
builder.Services.AddScoped<IProviderJobsService, ProviderJobsService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddHttpClient<IEtaProvider, EtaProvider>();

// SignalR notifiers live in the API layer; handlers depend only on the interfaces.
builder.Services.AddScoped<KHDMA.Application.Interfaces.RealTime.IBookingNotifier,
    KHDMA.API.Hubs.BookingNotifier>();
builder.Services.AddScoped<KHDMA.Application.Interfaces.RealTime.IChatNotifier,
    KHDMA.API.Hubs.ChatNotifier>();

// ---------------------------------------------------------------------------
// Options
// ---------------------------------------------------------------------------
builder.Services.Configure<DispatchSettings>(builder.Configuration.GetSection(DispatchSettings.SectionName));
builder.Services.Configure<VatSettings>(builder.Configuration.GetSection(VatSettings.SectionName));
builder.Services.Configure<FileUploadSettings>(builder.Configuration.GetSection(FileUploadSettings.SectionName));

// ---------------------------------------------------------------------------
// Distributed stores - Redis when configured, in-process otherwise.
//
// The in-memory implementations are correct for a single instance only. Running
// more than one API process on them silently breaks the first-accept guarantee,
// which is why startup logs a warning rather than failing quietly.
// ---------------------------------------------------------------------------
var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

if (redisEnabled && !string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        _ => ConnectionMultiplexer.Connect(redisConnectionString));

    builder.Services.AddSingleton<ILockService, RedisLockService>();
    builder.Services.AddSingleton<ILocationStore, RedisLocationStore>();
    builder.Services.AddSingleton<IPresenceStore, RedisPresenceStore>();
    builder.Services.AddSingleton<IDispatchCandidateStore, RedisDispatchCandidateStore>();

    Log.Information("Real-time state: Redis at {Endpoint}", redisConnectionString);
}
else
{
    builder.Services.AddSingleton<ILockService, InMemoryLockService>();
    builder.Services.AddSingleton<ILocationStore, InMemoryLocationStore>();
    builder.Services.AddSingleton<IPresenceStore, InMemoryPresenceStore>();
    builder.Services.AddSingleton<IDispatchCandidateStore, InMemoryDispatchCandidateStore>();

    Log.Warning(
        "Real-time state: in-memory. Single instance only - scale out requires Redis:Enabled=true.");
}

// ---------------------------------------------------------------------------
// Background workers
// ---------------------------------------------------------------------------
builder.Services.AddHostedService<DispatchTimeoutWorker>();
builder.Services.AddHostedService<ScheduledBookingWorker>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(KHDMA.Application.Interfaces.IBookingService).Assembly));
builder.Services.AddScoped<IAdminFinanceService, AdminFinanceService>();
builder.Services.AddHttpClient<IPaymobService, PaymobService>();

builder.Services.AddScoped<IAdminContentService, AdminContentService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

builder.Services.AddControllers();

// [ApiController] short-circuits a failed model binding with its own
// ValidationProblemDetails body, which is the one 400 that does not look like
// every other response. Re-shape it into ApiResponse so a client can parse any
// status the same way; the per-field errors are folded into the message.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kv => kv.Value?.Errors.Count > 0)
            .SelectMany(kv => kv.Value!.Errors.Select(e =>
                string.IsNullOrWhiteSpace(kv.Key)
                    ? e.ErrorMessage
                    : $"{kv.Key}: {e.ErrorMessage}"))
            .ToList();

        var message = errors.Count > 0 ? string.Join("; ", errors) : "Invalid request";
        return new BadRequestObjectResult(ApiResponse<object>.Fail(message));
    };
});

var signalR = builder.Services.AddSignalR();

// The backplane is what lets two API instances deliver an event to a client
// connected to the other one. Without it, a dispatched job card reaches only the
// providers who happen to share an instance with the dispatcher.
if (redisEnabled && !string.IsNullOrWhiteSpace(redisConnectionString))
    signalR.AddStackExchangeRedis(redisConnectionString, o => o.Configuration.ChannelPrefix = RedisChannel.Literal("khdma"));
builder.Services.AddEndpointsApiExplorer();
 
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "KHDMA API",
        Version = "v1",
        Description =
            "Endpoints are grouped by the role that calls them: **ADMIN** first, then **CUSTOMER**, " +
            "then **PROVIDER**, then the **COMMON** sections whose routes serve more than one role, " +
            "and finally the anonymous **PUBLIC** catalogue.\n\n" +
            "### Log in once as each role, then hold all three tokens at the same time\n" +
            "Open **Authorize** and fill every box. Each endpoint is wired to the box for its own " +
            "section, so an admin route sends the admin token and a provider route sends the provider " +
            "token - nothing has to be cleared and re-pasted between calls.\n\n" +
            "| Box | Feeds sections | Get the token from |\n" +
            "|---|---|---|\n" +
            "| `AdminBearer` | 01-11 ADMIN | `POST /api/auth/login/admin` |\n" +
            "| `CustomerBearer` | 12-15 CUSTOMER | `POST /api/auth/login` as a customer |\n" +
            "| `ProviderBearer` | 16-19 PROVIDER | `POST /api/auth/login` as a provider |\n" +
            "| `CommonBearer` | 20-25 COMMON | any of the three - these routes serve more than one role |\n\n" +
            "Paste only the `token.accessToken` value, without the word `Bearer` - Swagger adds it. " +
            "Section 26 needs no token at all.",
    });

    // Sections come from [Tags(ApiTags.X)] on the controller - or on the action,
    // where one controller serves several roles - and are ordered by the filter.
    c.TagActionsBy(ApiTags.SelectFor);
    c.DocumentFilter<TagOrderDocumentFilter>();

    // One scheme per role rather than one shared "Bearer". Swagger UI stores a
    // credential per scheme name, so four names is what lets four tokens be held
    // at once; RoleSecurityOperationFilter then picks the right one per endpoint.
    AddRoleScheme(RoleSecurityOperationFilter.Admin,
        "Admin token. Used by sections 01-11. Get it from POST /api/auth/login/admin.");
    AddRoleScheme(RoleSecurityOperationFilter.Customer,
        "Customer token. Used by sections 12-15. Get it from POST /api/auth/login.");
    AddRoleScheme(RoleSecurityOperationFilter.Provider,
        "Provider token. Used by sections 16-19. Get it from POST /api/auth/login.");
    AddRoleScheme(RoleSecurityOperationFilter.Common,
        "Any role's token. Used by sections 20-25, whose routes serve more than one role - " +
        "put the customer token here to exercise the customer side of chat and booking history, " +
        "or the provider token to exercise the provider side.");

    c.OperationFilter<RoleSecurityOperationFilter>();

    void AddRoleScheme(string name, string description) =>
        c.AddSecurityDefinition(name, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = description,
        });
});

var app = builder.Build();

// First in the pipeline so it also catches failures thrown by everything below it.
app.UseMiddleware<KHDMA.API.Middleware.ExceptionMiddleware>();
app.UseRequestLocalization();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DocumentTitle = "KHDMA API";
    // 25 sections is a long page - start collapsed, and give the reader a filter
    // box so typing "PROVIDER" leaves only that role's endpoints on screen.
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    c.EnableFilter();
    c.DefaultModelsExpandDepth(-1);
});
app.UseStaticFiles();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("AllowFrontends");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<KHDMA.API.Middleware.AuditMiddleware>();
app.MapHealthChecks("/health");
app.MapControllers();

// Mapped after UseAuthentication/UseAuthorization so [Authorize] on the hubs is
// actually enforced - mapping a hub before the auth middleware leaves it open.
app.MapHub<KHDMA.API.Hubs.BookingHub>("/hubs/booking");
app.MapHub<KHDMA.API.Hubs.ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
await KHDMA.Infrastructure.Data.AppDbSeeder.SeedAsync(app.Services);

app.Run();
