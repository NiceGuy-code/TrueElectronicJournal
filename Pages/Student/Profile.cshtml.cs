using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public ProfileModel(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? GroupName { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage("/Account/Login");

        await FillProfileAsync(user);
        return Page();
    }

    private async Task FillProfileAsync(ApplicationUser user)
    {
        FullName = user.FullName;
        Email = user.Email ?? "";
        GroupName = user.GroupId == null
            ? null
            : (await _db.Groups.FindAsync(user.GroupId))?.Name;
    }
}
