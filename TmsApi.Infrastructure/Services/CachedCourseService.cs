using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;

namespace TmsApi.Infrastructure.Services;

public class CachedCourseService(
    HybridCache cache,
    ICourseService service,
    ILogger<CachedCourseService> logger)
    : ICachedCourseService
{
public async Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            (service, code),
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);
                var course = await state.service.GetByCodeAsync(state.code, token)
                    ?? throw new NotFoundException($"Course {state.code} not found.");
                return new CourseResponseDto(
                    course.Id, course.Code, course.Title,
                    course.MaxCapacity, course.Enrollments.Count);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
            logger.LogInformation("Cache HIT for {Key}", key);

        return dto;
    }
   public async Task<PagedResponse<CourseResponseDto>> GetCoursesPageAsync(int page, int pageSize, CancellationToken ct)
    {
        var key = CacheKeys.CoursesPage(page, pageSize);
        var dbHit = false;

        var result = await cache.GetOrCreateAsync(
            key,
            (service, page, pageSize),
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key} fetching from DB", key);
                var request = new PagedRequest { Page = state.page, PageSize = state.pageSize };
                return await state.service.GetCoursesAsync(request, token);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
            logger.LogInformation("Cache HIT for {Key}", key);

        return result;
    }
    public async Task InvalidateCourseCacheAsync(CancellationToken ct)
    {
        logger.LogInformation("Invalidating cache tag {Tag}", CacheKeys.CoursesTag);
        await cache.RemoveByTagAsync(CacheKeys.CoursesTag, ct);
    }
}