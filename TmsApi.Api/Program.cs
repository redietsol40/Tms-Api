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
    options.AddPolicy("AllowAngular", policy =>
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
}

app.UseExceptionHandler();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<V1DeprecationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
//app.UseMiddleware<V1DeprecationMiddleware>();
app.UseCors("AllowAngular");


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
app.MapHub<TmsHub>("/hubs/tms");


app.Run();