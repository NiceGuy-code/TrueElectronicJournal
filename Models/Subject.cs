using System.ComponentModel.DataAnnotations;

namespace Journal.Models;

public class Subject
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public ApplicationUser? Teacher { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
