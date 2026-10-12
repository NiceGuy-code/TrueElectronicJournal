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
public class CalendarModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CalendarModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public WeekCalVm Cal { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(DateTime? date)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

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

        foreach (var l in lessons)
            l.Href = $"/Teacher/Lesson/{l.Id}";

        Cal.Lessons = lessons;
        return Page();
    }
}
