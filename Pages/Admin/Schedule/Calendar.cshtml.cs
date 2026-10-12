using Journal.Data;
using Journal.Pages.Shared;
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

    public WeekCalVm Cal { get; set; } = new();

    public async Task OnGetAsync(DateTime? date)
    {
        var anchor = (date ?? DateTime.Now).Date;

        // Корректный понедельник: Пн=0 ... Вс=6
        var weekStart = anchor.AddDays(-(((int)anchor.DayOfWeek + 6) % 7));
        var weekEnd = weekStart.AddDays(7);

        Cal = new WeekCalVm
        {
            WeekStart = weekStart,
            Today = DateTime.Today,
            WeekDays = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).ToList()
        };

        var lessons = await _db.Lessons
            .Where(l => l.DateFrom >= weekStart && l.DateFrom < weekEnd)
            .OrderBy(l => l.DateFrom)
            .Select(l => new WeekCalLesson
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject!.Name,
                GroupName = l.Group!.Name,
                TeacherName = l.Teacher!.FullName
            })
            .ToListAsync();

        foreach (var l in lessons)
            l.Href = $"/Admin/Schedule/Edit/{l.Id}";

        Cal.Lessons = lessons;
    }
}
