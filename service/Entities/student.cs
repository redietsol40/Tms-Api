namespace TmsApi.Entities;

public class Student
{
    public int Id { get; set; } // surrogate PK
    public required string RegistrationNumber { get; set; } // natural key
    public required string Name { get; set; }
    public decimal GPA { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
