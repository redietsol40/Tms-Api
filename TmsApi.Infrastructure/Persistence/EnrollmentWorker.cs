using Microsoft.Extensions.DependencyInjection;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Persistence;

public class EnrollmentWorker(IServiceScopeFactory scopeFactory)
{
    public void ProcessBatch()
    {
                using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

        
        var enrollmentCount = context.Enrollments.Count();
        Console.WriteLine($"[EnrollmentWorker] Processed batch. Current enrollment count: {enrollmentCount}");

       
    }
}
