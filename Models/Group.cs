using System.ComponentModel.DataAnnotations;

namespace Journal.Models;

public class Group
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
