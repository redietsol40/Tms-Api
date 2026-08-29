using System.Security.Claims;
using TmsApi.Api.Options;
using TmsApi.Api.TrainingAuth;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Asp.Versioning;
using FluentValidation;
using MediatR;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using TmsApi.Api.Filters;
using TmsApi.Application.Interfaces;
using TmsApi.Api.Middlewares;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using Microsoft.Extensions.Caching.Hybrid;
using TmsApi.Infrastructure.Services;
using TmsApi.Infrastructure.Transcripts;
using System.Threading.Channels;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Api.Hubs;
using TmsApi.Application.Notifications;
using TmsApi.Api.Notifications;
using TmsApi.Application.Auth;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

using TmsApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Api.Authorization;
using Microsoft.AspNetCore.RateLimiting;
 using System.Text;

 


 

var builder = WebApplication.CreateBuilder(args);


// ---------- SERVICES ----------

// Authentication / Authorization (M4 Session 1)
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

// DI lifetime validation (M4 Session 2 — Exercise 2)
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
builder.Services.AddAntiforgery(options =>
{
options.HeaderName = "X-XSRF-TOKEN";
});
// Background worker (M4 Session 2 — Exercise 2)
builder.Services.AddSingleton<EnrollmentWorker>();


// Options pattern — validated configuration (M4 Session 2 — Exercise 3)
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Controllers + global audit filter (M4 Session 3, M6 Session 2 — Exercise 4 Part D)
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
});
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});
// ProblemDetails (M4 Session 3 — Exercise 6)
builder.Services.AddProblemDetails();

// OpenAPI / Scalar — versioned documents (M7 Session 1 — Exercise 1)
builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.GroupName == "v1";
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
});

// API versioning (M7 Session 1 — Exercise 1)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// EF Core / PostgreSQL DbContext (M5 Session 1)
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information) // SQL logging (M5 Session 1, Exercise 2)
        .EnableSensitiveDataLogging()); // dev only
       builder.Services.AddCors(options =>
{
    options.AddPolicy("TmsClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// M6 Session 1 — Course and Enrollment services (DB-backed)
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// M7 Session 1 — MediatR, FluentValidation, pipeline
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EnrollStudentCommand>());
builder.Services.AddValidatorsFromAssemblyContaining<EnrollStudentValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});
builder.Services.AddIdentityCore<TmsUser>(options =>
{
// Enterprise Password Policy
options.Password.RequiredLength = 12;
options.Password.RequireUppercase = true;
options.Password.RequireDigit = true;
options.Password.RequireNonAlphanumeric = true;
// Brute-Force Lockout Protection
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<TmsDbContext>();
builder.Services.AddScoped<TokenService>();

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

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHybridCache();
builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();
builder.Services.AddHostedService<TranscriptWorker>();
builder.Services.AddSingleton(Channel.CreateBounded<TranscriptRequest>(
new BoundedChannelOptions(100)
{
FullMode = BoundedChannelFullMode.Wait
}));

builder.Services.AddSignalR();
builder.Services.AddSingleton<ITranscriptNotificationService, SignalRTranscriptNotificationService>();

var app = builder.Build();

// Seed the database on startup (dev convenience)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
app.MapGet("/api/dev/students", async (TmsDbContext db) =>
{
    var students = await db.Students.ToListAsync();
    return Results.Ok(students);
});}

app.MapGet("/api/dev/hash-test", () =>
{
    var service = new CryptoDemoService();

    string hash1 = service.HashUserPassword("Password123!");
    string hash2 = service.HashUserPassword("Password123!");

    bool match1 = service.VerifyUserPassword("Password123!", hash1);
    bool match2 = service.VerifyUserPassword("Password123!", hash2);

    return Results.Ok(new
    {
        hash1,
        hash2,
        hashesAreDifferent = hash1 != hash2,
        match1,
        match2
    });
});

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<V1DeprecationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<V1DeprecationMiddleware>();
app.Use(async (context, next) =>
{
if (context.User.Identity?.IsAuthenticated == true || context.
Request.Cookies.ContainsKey("tms_auth"))
{
    app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    await next();
});
var antiforgery = context.RequestServices
.GetRequiredService<IAntiforgery>();
var tokens = antiforgery.GetAndStoreTokens(context);
context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
new CookieOptions
{
HttpOnly = false, // MUST be false so Angular Jav

Secure = !builder.Environment.IsDevelopment(),
SameSite = SameSiteMode.Strict
});
}
await next(context);
});
app.UseCors("TmsClient");


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.AddDocument("v1");
        options.AddDocument("v2");
    });

}

app.MapControllers();
app.MapHub<TmsHub>("/hubs/tms")
    .RequireCors("TmsClient");
app.Run();