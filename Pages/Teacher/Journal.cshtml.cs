using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Teacher;

[Authorize(Roles = "Teacher")]
public class JournalModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public JournalModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public string GroupName { get; set; } = string.Empty;
    public List<GroupItem> Groups { get; set; } = new();
    public List<LessonHeader> Lessons { get; set; } = new();
    public List<StudentRowJournal> Students { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? groupId)
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

        var teacherGroupIds = await _db.Lessons
            .Where(l => l.TeacherId == teacher.Id)
            .Select(l => l.GroupId)
            .Distinct()
            .ToListAsync();

        Groups = await _db.Groups
            .Where(g => teacherGroupIds.Contains(g.Id))
            .OrderBy(g => g.Name)
            .Select(g => new GroupItem { Id = g.Id, Name = g.Name })
            .ToListAsync();

        // Нет занятий — дальше ничего не грузим
        if (Groups.Count == 0)
            return Page();

        if (groupId == null)
            groupId = Groups[0].Id;

        // Учитель видит только группы, где ведёт занятия
        if (!Groups.Any(g => g.Id == groupId))
            return NotFound();

        GroupName = Groups.First(g => g.Id == groupId).Name;

        var students = await _db.Users
            .Where(u => u.GroupId == groupId && !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var lessons = await _db.Lessons
            .Where(l => l.GroupId == groupId && l.TeacherId == teacher.Id)
            .OrderBy(l => l.DateFrom)
            .Select(l => new LessonHeader
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                Topic = l.Topic
            })
            .ToListAsync();

        Lessons = lessons;

        var lessonIds = lessons.Select(l => l.Id).ToList();

        var grades = await _db.Grades
            .Where(g => lessonIds.Contains(g.LessonId) && g.Type != GradeType.Homework)
            .ToListAsync();

        var lessonGrades = grades
            .Where(g => g.Type == GradeType.Lesson)
            .ToDictionary(g => $"{g.StudentId}_{g.LessonId}", g => g.Value);

        var absentSet = grades
            .Where(g => g.Type == GradeType.Absent)
            .Select(g => $"{g.StudentId}_{g.LessonId}")
            .ToHashSet();

        var naSet = grades
            .Where(g => g.Type == GradeType.NA)
            .Select(g => $"{g.StudentId}_{g.LessonId}")
            .ToHashSet();

        Students = students.Select(s => new StudentRowJournal
        {
            Id = s.Id,
            FullName = s.FullName,
            Cells = lessons.Select(l =>
            {
                var key = $"{s.Id}_{l.Id}";
                return new Cell
                {
                    Grade = lessonGrades.TryGetValue(key, out var v) ? v : null,
                    IsAbsent = absentSet.Contains(key),
                    IsNA = naSet.Contains(key)
                };
            }).ToList()
        }).ToList();

        return Page();
    }

    public class GroupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LessonHeader
    {
        public int Id { get; set; }
        public DateTime DateFrom { get; set; }
        public string? Topic { get; set; }
    }

    public class Cell
    {
        public int? Grade { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsNA { get; set; }
    }

    public class StudentRowJournal
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<Cell> Cells { get; set; } = new();
    }
}
