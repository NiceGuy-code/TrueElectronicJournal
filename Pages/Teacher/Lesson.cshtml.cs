using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Teacher;

[Authorize(Roles = "Teacher")]
public class LessonModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public LessonModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public string SubjectName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string? Topic { get; set; }

    [BindProperty]
    public List<string> AbsentIds { get; set; } = new();

    // Ключ — StudentId, значение — "2".."5" или "" (пустая строка безопасна при биндинге)
    [BindProperty]
    public Dictionary<string, string> Grades { get; set; } = new();

    [BindProperty]
    public List<string> NAIds { get; set; } = new();

    public List<StudentRow> Students { get; set; } = new();

    public bool HasHomework { get; set; } = false;
    public int? HomeworkId { get; set; }
    public string? HomeworkDescription { get; set; }
    public DateTime? HomeworkDeadline { get; set; }
    public int SubmittedCount { get; set; } = 0;
    public int TotalStudents { get; set; } = 0;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

        var lesson = await _db.Lessons
            .Include(l => l.Subject)
            .Include(l => l.Group)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null || lesson.TeacherId != teacher.Id)
            return NotFound();

        SubjectName = lesson.Subject == null ? "" : lesson.Subject.Name;
        GroupName = lesson.Group == null ? "" : lesson.Group.Name;
        DateFrom = lesson.DateFrom;
        DateTo = lesson.DateTo;
        Topic = lesson.Topic;

        var students = await _db.Users
            .Where(u => u.GroupId == lesson.GroupId && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        TotalStudents = students.Count;

        var hw = await _db.Homeworks.FirstOrDefaultAsync(h => h.LessonId == id);
        if (hw != null)
        {
            HasHomework = true;
            HomeworkId = hw.Id;
            HomeworkDescription = hw.Description ?? "—";
            HomeworkDeadline = hw.Deadline;
            SubmittedCount = await _db.StudentHomeworks.CountAsync(
                sh => sh.HomeworkId == hw.Id && !sh.Student!.IsDeleted);
        }

        var grades = await _db.Grades
            .Where(g => g.LessonId == id &&
                (g.Type == GradeType.Lesson || g.Type == GradeType.NA || g.Type == GradeType.Absent))
            .ToListAsync();

        var absentIds = grades.Where(g => g.Type == GradeType.Absent)
            .Select(g => g.StudentId).ToHashSet();
        var naIds = grades.Where(g => g.Type == GradeType.NA)
            .Select(g => g.StudentId).ToHashSet();
        var lessonValues = grades.Where(g => g.Type == GradeType.Lesson)
            .ToDictionary(g => g.StudentId, g => g.Value);

        Students = students.Select(s => new StudentRow
        {
            Id = s.Id,
            FullName = s.FullName,
            IsAbsent = absentIds.Contains(s.Id),
            IsNA = naIds.Contains(s.Id),
            Grade = lessonValues.TryGetValue(s.Id, out var v) ? v : null
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null || lesson.TeacherId != teacher.Id)
            return NotFound();

        // Снимаем прошлые отметки занятия (Homework не трогаем), затем ставим заново
        var oldGrades = await _db.Grades
            .Where(g => g.LessonId == id &&
                (g.Type == GradeType.Lesson || g.Type == GradeType.NA || g.Type == GradeType.Absent))
            .ToListAsync();
        _db.Grades.RemoveRange(oldGrades);

        var validIds = await _db.Users
            .Where(u => u.GroupId == lesson.GroupId && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        // Приоритет: Absent > NA > оценка
        foreach (var sid in validIds)
        {
            if (AbsentIds.Contains(sid))
            {
                _db.Grades.Add(new Grade
                {
                    LessonId = id,
                    StudentId = sid,
                    Type = GradeType.Absent,
                    Value = null
                });
            }
            else if (NAIds.Contains(sid))
            {
                _db.Grades.Add(new Grade
                {
                    LessonId = id,
                    StudentId = sid,
                    Type = GradeType.NA,
                    Value = null
                });
            }
            else if (Grades.TryGetValue(sid, out var raw) &&
                     int.TryParse(raw, out var val) && val is >= 2 and <= 5)
            {
                _db.Grades.Add(new Grade
                {
                    LessonId = id,
                    StudentId = sid,
                    Type = GradeType.Lesson,
                    Value = val
                });
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public class StudentRow
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsAbsent { get; set; }
        public bool IsNA { get; set; }
        public int? Grade { get; set; }
    }
}
