using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("1.0")]
public class CoursesController(
    TmsDbContext context,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = context.Courses.AsNoTracking();

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                EnrollmentCount = c.Enrollments.Count
            })
            .ToListAsync(ct);

        var totalPages =
            (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages,
            hasNext = page < totalPages,
            hasPrevious = page > 1
        });
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseRequest request,
        CancellationToken ct = default)
    {
        var course = await context.Courses
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (course == null)
        {
            return NotFound();
        }

        var authResult =
            await authorizationService.AuthorizeAsync(
                User,
                course,
                "CanEditCourse");

        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        course.Code = request.Code;
        course.Title = request.Title;
        course.MaxCapacity = request.MaxCapacity;

        await context.SaveChangesAsync(ct);

        return NoContent();
    }
}