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

    public List<GradeRow> RecentGrades { get; set; } = new();
    public List<LessonRow> UpcomingLessons { get; set; } = new();
    public List<AnnouncementRow> RecentAnnouncements { get; set; } = new();

    // Средний балл (без Н/Б) и посещаемость (% занятий без прогулов)
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

        // Прогулы (Н) — единственное, что уменьшает посещаемость; УП/ОП/н/б не влияют
        Absences = await _db.Grades
            .CountAsync(g => g.StudentId == student.Id
                && g.Type == GradeType.Lesson
                && g.Attendance == AttendanceType.Unexcused);

        TotalLessons = student.GroupId == null
            ? 0
            : await _db.Lessons.CountAsync(l => l.GroupId == student.GroupId);

        Attendance = TotalLessons == 0
            ? null
            : Math.Max(0, 1.0 - ((double)Absences / TotalLessons)) * 100;

        RecentGrades = await _db.Grades
            .Where(g => g.StudentId == student.Id && g.Type != GradeType.Homework)
            .OrderByDescending(g => g.Lesson!.DateFrom)
            .Take(5)
            .Select(g => new GradeRow
            {
                Date = g.Lesson!.DateFrom,
                SubjectName = g.Lesson.Subject!.Name,
                Value = g.Value,
                Type = g.Type,
                Attendance = g.Attendance
            })
            .ToListAsync();

        if (student.GroupId != null)
        {
            UpcomingLessons = await _db.Lessons
                .Where(l => l.GroupId == student.GroupId && l.DateFrom >= DateTime.Now)
                .OrderBy(l => l.DateFrom)
                .Take(3)
                .Select(l => new LessonRow
                {
                    DateFrom = l.DateFrom,
                    SubjectName = l.Subject!.Name,
                    TeacherName = l.Teacher!.FullName
                })
                .ToListAsync();
        }

        RecentAnnouncements = await _db.Announcements
            .Where(a => a.GroupId == null || a.GroupId == student.GroupId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .Select(a => new AnnouncementRow { Title = a.Title, CreatedAt = a.CreatedAt })
            .ToListAsync();

        return Page();
    }

    public class GradeRow
    {
        public DateTime Date { get; set; }
        public string SubjectName { get; set; } = "";
        public int? Value { get; set; }
        public GradeType Type { get; set; }
        public AttendanceType? Attendance { get; set; }
    }

    public class LessonRow
    {
        public DateTime DateFrom { get; set; }
        public string SubjectName { get; set; } = "";
        public string TeacherName { get; set; } = "";
    }

    public class AnnouncementRow
    {
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
