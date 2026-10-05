using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Teacher;

[Authorize(Roles = "Teacher")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<LessonRow> Items { get; set; } = new();

    // Task<IActionResult>: нужен return RedirectToPage при отсутствии учителя
    public async Task<IActionResult> OnGetAsync()
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

        Items = await _db.Lessons
            .Where(l => l.TeacherId == teacher.Id)
            .OrderByDescending(l => l.DateFrom)
            .Select(l => new LessonRow
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject == null ? "" : l.Subject.Name,
                GroupName = l.Group == null ? "" : l.Group.Name,
                Topic = string.IsNullOrEmpty(l.Topic) ? "—" : l.Topic
            })
            .ToListAsync();

        return Page();
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}
