using Journal.Data;
using Journal.Pages.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Schedule;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<LessonRow> Items { get; set; } = new();
    public WeekCalVm Cal { get; set; } = new();
    public bool ShowList { get; private set; }

    // По умолчанию — календарь недели; ?view=list — таблица-список
    public async Task OnGetAsync(DateTime? date, string? view)
    {
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
                .OrderByDescending(l => l.DateFrom)
                .Select(l => new LessonRow
                {
                    Id = l.Id,
                    DateFrom = l.DateFrom,
                    DateTo = l.DateTo,
                    SubjectName = l.Subject == null ? "" : l.Subject.Name,
                    GroupName = l.Group == null ? "" : l.Group.Name,
                    TeacherName = l.Teacher.FullName,
                    Topic = l.Topic
                })
                .ToListAsync();
            return;
        }

        var weekLessons = await _db.Lessons
            .Where(l => l.DateFrom >= weekStart && l.DateFrom < weekEnd)
            .OrderBy(l => l.DateFrom)
            .Select(l => new WeekCalLesson
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject == null ? "" : l.Subject.Name,
                GroupName = l.Group == null ? "" : l.Group.Name,
                TeacherName = l.Teacher.FullName,
                Topic = l.Topic ?? ""
            })
            .ToListAsync();

        foreach (var l in weekLessons)
            l.Href = $"/Admin/Schedule/Edit/{l.Id}";

        Cal.Lessons = weekLessons;
    }

    // Удаление: нельзя, если по занятию есть оценки или ДЗ
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var lesson = await _db.Lessons.FindAsync(id);
        if (lesson == null)
            return Redirect("/Admin/Schedule");

        if (await _db.Grades.AnyAsync(g => g.LessonId == id)
            || await _db.Homeworks.AnyAsync(h => h.LessonId == id))
        {
            TempData["Error"] = "Нельзя удалить занятие: есть оценки или ДЗ";
            return Redirect("/Admin/Schedule");
        }

        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync();
        return Redirect("/Admin/Schedule");
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string? Topic { get; set; }
    }
}
