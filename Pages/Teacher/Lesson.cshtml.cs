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

    // Ключ — StudentId, значение — "Excused"/"Unexcused"/"Late" или "" (пустая строка безопасна при биндинге)
    [BindProperty]
    public Dictionary<string, string> Attendance { get; set; } = new();

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
                (g.Type == GradeType.Lesson || g.Type == GradeType.NA))
            .ToListAsync();

        var unexcusedIds = grades.Where(g => g.Type == GradeType.Lesson
                && g.Attendance == AttendanceType.Unexcused)
            .Select(g => g.StudentId).ToHashSet();
        var excusedIds = grades.Where(g => g.Type == GradeType.Lesson
                && g.Attendance == AttendanceType.Excused)
            .Select(g => g.StudentId).ToHashSet();
        var lateIds = grades.Where(g => g.Type == GradeType.Lesson
                && g.Attendance == AttendanceType.Late)
            .Select(g => g.StudentId).ToHashSet();
        var naIds = grades.Where(g => g.Type == GradeType.NA)
            .Select(g => g.StudentId).ToHashSet();
        var lessonValues = grades.Where(g => g.Type == GradeType.Lesson)
            .ToDictionary(g => g.StudentId, g => g.Value);

        Students = students.Select(s => new StudentRow
        {
            Id = s.Id,
            FullName = s.FullName,
            AttendanceCode = unexcusedIds.Contains(s.Id) ? "Н"
                : excusedIds.Contains(s.Id) ? "УП"
                : lateIds.Contains(s.Id) ? "ОП"
                : "",
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
                (g.Type == GradeType.Lesson || g.Type == GradeType.NA))
            .ToListAsync();
        _db.Grades.RemoveRange(oldGrades);

        var validIds = await _db.Users
            .Where(u => u.GroupId == lesson.GroupId && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        // Приоритет: Н/Б > посещение/оценка
        foreach (var sid in validIds)
        {
            int? val = null;
            if (Grades.TryGetValue(sid, out var raw) &&
                int.TryParse(raw, out var parsed) && parsed is >= 2 and <= 5)
            {
                val = parsed;
            }

            Attendance.TryGetValue(sid, out var attRaw);
            var att = Enum.TryParse<AttendanceType>(attRaw, out var attVal)
                ? (AttendanceType?)attVal
                : null;

            if (NAIds.Contains(sid))
            {
                _db.Grades.Add(new Grade
                {
                    LessonId = id,
                    StudentId = sid,
                    Type = GradeType.NA,
                    Value = null,
                    Attendance = null
                });
            }
            else if (val != null || (att != null && att != AttendanceType.Present))
            {
                _db.Grades.Add(new Grade
                {
                    LessonId = id,
                    StudentId = sid,
                    Type = GradeType.Lesson,
                    Value = val,
                    Attendance = att ?? AttendanceType.Present
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
        public string AttendanceCode { get; set; } = string.Empty;
        public bool IsNA { get; set; }
        public int? Grade { get; set; }
    }
}
