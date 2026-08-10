namespace TmsApi.Infrastructure.Caching;

public static class CacheKeys
{
    public const string CoursesTag = "courses";

    public static string Course(string code)
    {
        return $"course-{code}";
    }

    public static string CoursesPage(int page, int pageSize)
    {
        return $"courses-page-{page}-{pageSize}";
    }
}