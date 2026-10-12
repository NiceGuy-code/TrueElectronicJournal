using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Schedule;

[Authorize(Roles = "Admin")]
public class CalendarModel : PageModel
{
    private readonly AppDbContext _db;

    public CalendarModel(AppDbContext db)
    {
        _db = db;
    }

    public DateTime WeekStart { get; set; }
    public List<DateTime> WeekDays { get; set; } = new();
    public List<LessonRow> Lessons { get; set; } = new();
    public DateTime Today { get; set; }

    public async Task OnGetAsync(DateTime? date)
    {
        var anchor = (date ?? DateTime.Now).Date;

        // Корректный понедельник: Пн=0 ... Вс=6
        WeekStart = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var weekEnd = WeekStart.AddDays(7);

        WeekDays = Enumerable.Range(0, 7).Select(i => WeekStart.AddDays(i)).ToList();
        Today = DateTime.Today;

        Lessons = await _db.Lessons
            .Where(l => l.DateFrom >= WeekStart && l.DateFrom < weekEnd)
            .OrderBy(l => l.DateFrom)
            .Select(l => new LessonRow
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject!.Name,
                GroupName = l.Group!.Name,
                TeacherName = l.Teacher!.FullName
            })
            .ToListAsync();

        return;
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SubjectName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string TeacherName { get; set; } = "";
    }
}
