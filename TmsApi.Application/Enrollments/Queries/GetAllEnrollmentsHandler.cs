using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public class GetAllEnrollmentsHandler(IEnrollmentService enrollmentService)
    : IRequestHandler<GetAllEnrollmentsQuery, List<EnrollmentSummaryDto>>
{
    public Task<List<EnrollmentSummaryDto>> Handle(GetAllEnrollmentsQuery query, CancellationToken ct)
        => enrollmentService.GetAllAsync(ct);
}