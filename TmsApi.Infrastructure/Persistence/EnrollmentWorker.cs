using TmsApi.Data;

namespace TmsApi;

using TmsApi;

public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
        // Create a short-lived scope so we get a fresh, request-independent
        // scoped DbContext instance — never capture a scoped service directly
        // inside a singleton.
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

        // Example batch work — adjust to whatever the worker actually needs to do.
        var enrollmentCount = context.Enrollments.Count();
        Console.WriteLine($"[EnrollmentWorker] Processed batch. Current enrollment count: {enrollmentCount}");

        // The 'using' block disposes the scope (and its scoped services,
        // including the DbContext) automatically when this method returns.
    }
}