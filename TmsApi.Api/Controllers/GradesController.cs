using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GradesController : ControllerBase
{
    private readonly TmsDbContext _context;

    public GradesController(TmsDbContext context)
    {
        _context = context;
    }

    public record GradeDto(int CourseId, string CourseTitle, decimal? Grade, bool IsGraded);

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyGrades()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student == null)
        {
            return NotFound(new { detail = "No student record found for this account." });
        }

        var grades = await _context.Enrollments
            .Where(e => e.StudentId == student.Id && !e.IsArchived)
            .Include(e => e.Course)
            .Select(e => new GradeDto(
                e.CourseId,
                e.Course.Title,
                e.Grade,
                e.Grade != null
            ))
            .ToListAsync();

        return Ok(grades);
    }
}