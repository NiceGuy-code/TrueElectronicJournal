using Journal.Data;
using Journal.Models;
using Journal.Pages.Shared;
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
    public WeekCalVm Cal { get; set; } = new();
    public bool NoGroup { get; set; } = false;
    public bool ShowList { get; private set; }

    // По умолчанию — календарь недели; ?view=list — таблица-список
    public async Task<IActionResult> OnGetAsync(DateTime? date, string? view)
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        ShowList = string.Equals(view, "list", StringComparison.OrdinalIgnoreCase);

        var anchor = (date ?? DateTime.Now).Date;
        var weekStart = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(7);

        Cal = new WeekCalVm
        {
            WeekStart = weekStart,
            Today = DateTime.Today,
            WeekDays = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList()
        };

        if (student.GroupId == null)
        {
            NoGroup = true;
            return Page();
        }

        if (ShowList)
        {
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

        var weekLessons = await _db.Lessons
            .Where(l => l.GroupId == student.GroupId
                && l.DateFrom >= weekStart && l.DateFrom < weekEnd)
            .OrderBy(l => l.DateFrom)
            .Select(l => new WeekCalLesson
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject!.Name,
                TeacherName = l.Teacher!.FullName
            })
            .ToListAsync();

        // У студента нет страницы занятия — клик ведёт обратно в список
        foreach (var l in weekLessons)
            l.Href = "/Student/Schedule";

        Cal.Lessons = weekLessons;
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
