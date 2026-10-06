using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Journal.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public int? GroupId { get; set; }

    // Мягкое удаление: запись остаётся, в списках и на входе не показывается
    public bool IsDeleted { get; set; } = false;

    public Group? Group { get; set; }

    public ICollection<StudentHomework> SubmittedHomeworks { get; set; } = new List<StudentHomework>();
}
