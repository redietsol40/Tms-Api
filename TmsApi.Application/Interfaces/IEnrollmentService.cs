using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;

public interface IEnrollmentService
{
    // ── M6: single enrolment lookup ──────────────────────────────
    Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);

    // ── M6: list enrolments for a course ─────────────────────────
    Task<List<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);

    // ── M6: nested create (kept for backward compat if still used) ─
    Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct);

    // ── M7: does this student already hold this course? ──────────
    Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct);

    // ── M7: raw entity insert used by EnrollStudentHandler ───────
    Task AddAsync(Enrollment enrollment, CancellationToken ct);

    // ── M7: schedule query — all enrolments for a student ─────────
    Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct);
}
