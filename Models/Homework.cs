using System.ComponentModel.DataAnnotations;

namespace Journal.Models;

public class Homework
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? FilePath { get; set; }

    public DateTime? Deadline { get; set; }

    public Lesson? Lesson { get; set; }
}
