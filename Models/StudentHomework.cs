using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Journal.Models;

public class StudentHomework
{
    public int Id { get; set; }

    public int HomeworkId { get; set; }
    [ForeignKey(nameof(HomeworkId))]
    public Homework? Homework { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;
    [ForeignKey(nameof(StudentId))]
    public ApplicationUser? Student { get; set; }

    public string? FilePath { get; set; }

    public DateTime SubmittedAt { get; set; }
}
