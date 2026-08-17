namespace TmsApi.Application.DTOs;

public record EnrollmentSummaryDto(
    int Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseName,
    string Status,
    DateTime EnrolledAt);