using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;

    public IndexModel(UserManager<ApplicationUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public string? GroupName { get; set; }
    public double? AverageGrade { get; set; }
    public double? Attendance { get; set; }
    public int GradesCount { get; set; }
    public int TotalLessons { get; set; }
    public int Absences { get; set; }

    // Средний балл (без Н/Б) и посещаемость (% занятий без Н/Б)
    public async Task<IActionResult> OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        GroupName = student.GroupId == null
            ? null
            : (await _db.Groups.FindAsync(student.GroupId))?.Name;

        AverageGrade = await _db.Grades
            .Where(g => g.StudentId == student.Id
                && (g.Type == GradeType.Lesson || g.Type == GradeType.Homework)
                && g.Value != null)
            .AverageAsync(g => (double?)g.Value);

        GradesCount = await _db.Grades
            .CountAsync(g => g.StudentId == student.Id
                && (g.Type == GradeType.Lesson || g.Type == GradeType.Homework)
                && g.Value != null);

        Absences = await _db.Grades
            .CountAsync(g => g.StudentId == student.Id && g.Type == GradeType.NA);

        TotalLessons = student.GroupId == null
            ? 0
            : await _db.Lessons.CountAsync(l => l.GroupId == student.GroupId);

        Attendance = TotalLessons == 0
            ? null
            : Math.Max(0, 1.0 - ((double)Absences / TotalLessons)) * 100;

        return Page();
    }
}
