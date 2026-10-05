using System.ComponentModel.DataAnnotations;

namespace Journal.Models;

public class Lesson
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int GroupId { get; set; }

    // Учитель привязан к занятию, а не к предмету
    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    [MaxLength(200)]
    public string? Topic { get; set; }

    public Subject? Subject { get; set; }

    public Group? Group { get; set; }

    public ApplicationUser Teacher { get; set; } = null!;

    public ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public ICollection<Homework> Homeworks { get; set; } = new List<Homework>();
}
