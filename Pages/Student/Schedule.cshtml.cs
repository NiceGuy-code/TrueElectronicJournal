using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class ScheduleModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ScheduleModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<LessonRow> Items { get; set; } = new();
    public bool NoGroup { get; set; } = false;

    public async Task<IActionResult> OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        if (student.GroupId == null)
        {
            NoGroup = true;
            return Page();
        }

        Items = await _db.Lessons
            .Where(l => l.GroupId == student.GroupId)
            .OrderBy(l => l.DateFrom)
            .Select(l => new LessonRow
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject!.Name,
                TeacherName = l.Teacher.FullName,
                Topic = l.Topic ?? "—"
            }).ToListAsync();

        return Page();
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }
}
