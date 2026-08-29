namespace TmsApi.Domain.Entities;

public class Course
{
    public int Id { get; set; } // surrogate PK
    public required string Code { get; set; } // natural key
    public required string Title { get; set; }

    public int MaxCapacity { get; set; }

    public string? InstructorId { get; set; } // FK to TmsUser.Id (Identity user)

    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}