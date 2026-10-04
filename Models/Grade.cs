using System.ComponentModel.DataAnnotations;

namespace Journal.Models;

public enum GradeType
{
    Lesson,
    Homework,
    NA
}

public class Grade
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;

    public int LessonId { get; set; }

    public int? Value { get; set; }

    public GradeType Type { get; set; } = GradeType.Lesson;

    public ApplicationUser? Student { get; set; }

    public Lesson? Lesson { get; set; }
}
