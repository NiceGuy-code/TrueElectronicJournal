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
    public WeekCalVm Cal { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(DateTime? date)
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
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

        if (student.GroupId == null)
        {
            NoGroup = true;
            return Page();
        }

        var lessons = await _db.Lessons
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

        foreach (var l in lessons)
            l.Href = "/Student/Schedule";

        Cal.Lessons = lessons;
        return Page();
    }
}
