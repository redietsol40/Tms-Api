using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence;

public class CourseService(TmsDbContext context) : ICourseService
{
    // M6 Session 2: paginated, filtered course list
    public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 50);
        var page = Math.Max(1, request.Page);
        var baseQuery = context.Courses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            baseQuery = baseQuery.Where(c =>
                c.Title.Contains(request.Search) || c.Code.Contains(request.Search));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<CourseResponseDto>
{
    Items = items,
    TotalCount = totalCount,
    Page = page,
    PageSize = pageSize
};
    }

    // M6 Session 1: single course lookup by id
    public async Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await context.Courses
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CourseResponseDto(
                c.Id,
                c.Code,
                c.Title,
                c.MaxCapacity,
                c.Enrollments.Count))
            .FirstOrDefaultAsync(ct);
    }

    // M6 Session 1: duplicate-code check for 409 on create
    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        return await context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);
    }

    // M6 Session 1: create a course
    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Code = request.Code,
            Title = request.Title,
            MaxCapacity = request.MaxCapacity
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);

        return new CourseResponseDto(
            course.Id,
            course.Code,
            course.Title,
            course.MaxCapacity,
            0);
    }

    // M7 Exercise 2: lookup by code, with enrolments included
    // Required by EnrollStudentHandler for the capacity check
    // (course.Enrollments.Count >= course.MaxCapacity)
    public async Task<Course?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return await context.Courses
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Code == code, ct);
    }
}
