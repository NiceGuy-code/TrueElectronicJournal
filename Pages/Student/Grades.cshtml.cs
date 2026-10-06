using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class GradesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GradesModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Row> Items { get; set; } = new();
    public bool NoGroup { get; set; } = false;

    public async Task<IActionResult> OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        if (student.GroupId == null)
        {
            NoGroup = true;
            return Page();
        }

        var lessons = await _db.Lessons
            .Where(l => l.GroupId == student.GroupId)
            .OrderByDescending(l => l.DateFrom)
            .Select(l => new { l.Id, l.DateFrom, l.Topic, SubjectName = l.Subject.Name })
            .ToListAsync();
        var lessonIds = lessons.Select(l => l.Id).ToList();

        var grades = await _db.Grades
            .Where(g => g.StudentId == student.Id
                && lessonIds.Contains(g.LessonId)
                && g.Type != GradeType.Homework)
            .ToListAsync();

        var gradeValues = grades.Where(g => g.Type == GradeType.Lesson)
            .ToDictionary(g => g.LessonId, g => g.Value);
        var naSet = grades.Where(g => g.Type == GradeType.NA)
            .Select(g => g.LessonId).ToHashSet();
        var absentSet = grades.Where(g => g.Type == GradeType.Absent)
            .Select(g => g.LessonId).ToHashSet();

        Items = lessons.Select(l => new Row
        {
            Date = l.DateFrom,
            SubjectName = l.SubjectName,
            Topic = l.Topic ?? "—",
            Grade = gradeValues.TryGetValue(l.Id, out var v) ? v : null,
            Status = naSet.Contains(l.Id) ? "н/б"
                   : absentSet.Contains(l.Id) ? "н"
                   : ""
        }).ToList();

        return Page();
    }

    public class Row
    {
        public DateTime Date { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public int? Grade { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
