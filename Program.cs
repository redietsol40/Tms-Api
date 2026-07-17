using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using TmsApi; // adjust namespace if needed

var builder = WebApplication.CreateBuilder(args);

// ✅ Register authentication + authorization services
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// ✅ Middleware order
app.UseMiddleware<RequestLoggingMiddleware>();   // custom logging wrapper
app.UseExceptionHandler("/error");               // error handling early
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Protected endpoint
app.MapGet("/api/assessments/results", () => Results.Ok(new
{
    courseCode = "CS-101",
    studentId = "S-001",
    letterGrade = "A"
}))
.RequireAuthorization();

app.Run();
