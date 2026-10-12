using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class CalendarModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CalendarModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public bool NoGroup { get; set; }
    public DateTime WeekStart { get; set; }
    public List<DateTime> WeekDays { get; set; } = new();
    public List<LessonRow> Lessons { get; set; } = new();
    public DateTime Today { get; set; }

    public async Task<IActionResult> OnGetAsync(DateTime? date)
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        if (student.GroupId == null)
        {
            NoGroup = true;
            return Page();
        }

        var anchor = (date ?? DateTime.Now).Date;

        // Корректный понедельник: Пн=0 ... Вс=6
        WeekStart = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var weekEnd = WeekStart.AddDays(7);

        WeekDays = Enumerable.Range(0, 7).Select(i => WeekStart.AddDays(i)).ToList();
        Today = DateTime.Today;

        Lessons = await _db.Lessons
            .Where(l => l.GroupId == student.GroupId
                && l.DateFrom >= WeekStart && l.DateFrom < weekEnd)
            .OrderBy(l => l.DateFrom)
            .Select(l => new LessonRow
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject!.Name,
                TeacherName = l.Teacher!.FullName
            })
            .ToListAsync();

        return Page();
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SubjectName { get; set; } = "";
        public string TeacherName { get; set; } = "";
    }
}
