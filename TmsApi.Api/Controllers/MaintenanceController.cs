using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/maintenance")]
public class MaintenanceController(TmsDbContext context) : ControllerBase
{
    // Bulk-archive enrollments older than a cutoff — one SQL UPDATE, no row loading
    [HttpPost("archive-old-enrollments")]
    public async Task<IActionResult> ArchiveOldEnrollments(
        [FromQuery] int daysOld = 365,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysOld);

        var affected = await context.Enrollments
            .Where(e => e.EnrolledAt < cutoff && !e.IsArchived)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.IsArchived, true),
                cancellationToken);

        return Ok(new { ArchivedCount = affected });
    }

    // Admin restore — bypasses the soft-delete filter
    [HttpGet("students/all-including-deleted")]
    public async Task<IActionResult> GetAllIncludingDeleted(CancellationToken cancellationToken)
    {
        var students = await context.Students
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    // Soft-delete a student
    [HttpDelete("students/{id}/soft-delete")]
    public async Task<IActionResult> SoftDeleteStudent(int id, CancellationToken cancellationToken)
    {
        var student = await context.Students.FindAsync([id], cancellationToken);
        if (student is null) return NotFound();

        student.IsDeleted = true;
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
