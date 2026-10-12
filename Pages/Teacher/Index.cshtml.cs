using Journal.Data;
using Journal.Models;
using Journal.Pages.Shared;
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
    public WeekCalVm Cal { get; set; } = new();
    public bool ShowList { get; private set; }

    // Task<IActionResult>: нужен return RedirectToPage при отсутствии учителя
    // По умолчанию — календарь недели; ?view=list — таблица-список
    public async Task<IActionResult> OnGetAsync(DateTime? date, string? view)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
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

        if (ShowList)
        {
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

        var weekLessons = await _db.Lessons
            .Where(l => l.TeacherId == teacher.Id
                && l.DateFrom >= weekStart && l.DateFrom < weekEnd)
            .OrderBy(l => l.DateFrom)
            .Select(l => new WeekCalLesson
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject!.Name,
                GroupName = l.Group!.Name,
                Topic = l.Topic ?? ""
            })
            .ToListAsync();

        foreach (var l in weekLessons)
            l.Href = $"/Teacher/Lesson/{l.Id}";

        Cal.Lessons = weekLessons;
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
