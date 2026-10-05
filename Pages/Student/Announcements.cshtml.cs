using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class AnnouncementsModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public AnnouncementsModel(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public List<Announcement> Items { get; set; } = new();

    // Общие объявления + объявления своей группы, свежие сверху
    public async Task<IActionResult> OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        Items = await _db.Announcements
            .Include(a => a.Author)
            .Include(a => a.Group)
            .Where(a => a.GroupId == null || a.GroupId == student.GroupId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Page();
    }
}
