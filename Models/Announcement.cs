using System.ComponentModel.DataAnnotations;

namespace Journal.Models;

public class Announcement
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Text { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string AuthorId { get; set; } = string.Empty;

    public int? GroupId { get; set; }

    public string? ImagePath { get; set; }

    public ApplicationUser? Author { get; set; }

    public Group? Group { get; set; }
}
