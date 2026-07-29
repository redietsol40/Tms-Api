namespace TmsApi.Domain.Entities;

public class Certificate
{
    public int Id { get; set; } // surrogate PK
    public required string SerialNumber { get; set; } // natural key
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public int StudentId { get; set; }
    public int CourseId { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
