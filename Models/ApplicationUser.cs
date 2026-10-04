using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Journal.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public int? GroupId { get; set; }

    public Group? Group { get; set; }
}
