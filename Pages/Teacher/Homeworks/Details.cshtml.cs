using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Teacher.Homeworks;

[Authorize(Roles = "Teacher")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public HomeworkInfo Info { get; set; } = new();
    public List<StudentRow> Students { get; set; } = new();

    // Ключ — StudentId, значение — "2".."5" или "" (пустая строка безопасна при биндинге)
    [BindProperty]
    public Dictionary<string, string> Grades { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

        var hw = await _db.Homeworks
            .Include(h => h.Lesson!).ThenInclude(l => l.Subject)
            .Include(h => h.Lesson!).ThenInclude(l => l.Group)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hw == null || hw.Lesson == null || hw.Lesson.TeacherId != teacher.Id)
            return NotFound();

        Info = new HomeworkInfo
        {
            LessonId = hw.LessonId,
            LessonDate = hw.Lesson.DateFrom,
            SubjectName = hw.Lesson.Subject?.Name ?? "",
            Topic = hw.Lesson.Topic ?? "—",
            Description = hw.Description ?? "—",
            FilePath = hw.FilePath,
            Deadline = hw.Deadline,
            GroupName = hw.Lesson.Group?.Name ?? ""
        };

        var students = await _db.Users
            .Where(u => u.GroupId == hw.Lesson.GroupId && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var submissions = await _db.StudentHomeworks
            .Where(sh => sh.HomeworkId == id)
            .ToListAsync();
        var subByStudent = submissions.ToDictionary(sh => sh.StudentId);

        var grades = await _db.Grades
            .Where(g => g.LessonId == hw.LessonId && g.Type == GradeType.Homework)
            .ToListAsync();
        var gradeByStudent = grades.ToDictionary(g => g.StudentId, g => g.Value);

        Students = students.Select(s => new StudentRow
        {
            Id = s.Id,
            FullName = s.FullName,
            FilePath = subByStudent.TryGetValue(s.Id, out var sub) ? sub.FilePath : null,
            SubmittedAt = subByStudent.TryGetValue(s.Id, out var sub2) ? sub2.SubmittedAt : null,
            Grade = gradeByStudent.TryGetValue(s.Id, out var g) ? g : null
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

        var hw = await _db.Homeworks
            .Include(h => h.Lesson)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hw == null || hw.Lesson == null || hw.Lesson.TeacherId != teacher.Id)
            return NotFound();

        var studentIds = await _db.Users
            .Where(u => u.GroupId == hw.Lesson.GroupId && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        // Снимаем прошлые оценки за ДЗ, затем ставим заново
        var oldGrades = await _db.Grades
            .Where(g => g.LessonId == hw.LessonId
                && g.Type == GradeType.Homework
                && studentIds.Contains(g.StudentId))
            .ToListAsync();
        _db.Grades.RemoveRange(oldGrades);

        foreach (var sid in studentIds)
        {
            if (Grades.TryGetValue(sid, out var raw)
                && int.TryParse(raw, out var val)
                && val is >= 2 and <= 5)
            {
                _db.Grades.Add(new Grade
                {
                    LessonId = hw.LessonId,
                    StudentId = sid,
                    Type = GradeType.Homework,
                    Value = val
                });
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public class HomeworkInfo
    {
        public int LessonId { get; set; }
        public DateTime LessonDate { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public DateTime? Deadline { get; set; }
        public string GroupName { get; set; } = string.Empty;
    }

    public class StudentRow
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? Grade { get; set; }
    }
}
