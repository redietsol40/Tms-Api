using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(TmsDbContext context) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var student = await context.Students.FindAsync(id);
        return student is not null ? Ok(student) : NotFound();
    }

    public record UpdateNameRequest(string Name);
    public record UpdateGpaRequest(decimal Gpa);

    [HttpPut("{id}/name")]
    public async Task<IActionResult> UpdateName(int id, [FromBody] UpdateNameRequest request)
    {
        var student = await context.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.Name = request.Name;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was modified by another user. Reload and try again.");
        }

        return Ok(student);
    }

    [HttpPut("{id}/gpa")]
    public async Task<IActionResult> UpdateGpa(int id, [FromBody] UpdateGpaRequest request)
    {
        var student = await context.Students.FindAsync(id);
        if (student is null) return NotFound();

        student.GPA = request.Gpa;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The record was modified by another user. Reload and try again.");
        }

        return Ok(student);
    }
}
