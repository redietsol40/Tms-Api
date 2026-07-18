using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ✅ Authentication + Authorization services
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

// ✅ Options pattern with validation
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ✅ Service registrations
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddSingleton<EnrollmentWorker>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

var app = builder.Build();

// ✅ Middleware order
app.UseMiddleware<RequestLoggingMiddleware>();   // correlation + logs
app.UseExceptionHandler("/error");               // error handling early
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Protected endpoint (from Session 1)
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

// ✅ Optional smoke-test route for worker (Session 2 Exercise 2)
app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
{
    worker.ProcessBatch();
    return Results.Ok("processed");
});

app.Run();
