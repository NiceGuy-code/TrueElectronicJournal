using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Teacher.Homeworks;

[Authorize(Roles = "Teacher")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<GroupItem> Groups { get; set; } = new();
    public string GroupName { get; set; } = string.Empty;
    public int? SelectedGroupId { get; set; }
    public List<HomeworkRow> Items { get; set; } = new();

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

        if (Groups.Count == 0)
            return Page();

        if (groupId == null)
            groupId = Groups[0].Id;

        if (!Groups.Any(g => g.Id == groupId))
            return NotFound();

        SelectedGroupId = groupId;
        GroupName = Groups.First(g => g.Id == groupId).Name;

        Items = await _db.Homeworks
            .Where(h => h.Lesson!.GroupId == groupId && h.Lesson.TeacherId == teacher.Id)
            .OrderByDescending(h => h.Lesson!.DateFrom)
            .Select(h => new HomeworkRow
            {
                Id = h.Id,
                LessonId = h.LessonId,
                LessonDate = h.Lesson!.DateFrom,
                SubjectName = h.Lesson.Subject!.Name,
                Description = h.Description ?? "—",
                LessonTopic = h.Lesson.Topic ?? "—",
                SubmittedCount = h.StudentHomeworks.Count(sh => !sh.Student!.IsDeleted),
                TotalStudents = h.Lesson.Group!.Students.Count(s => !s.IsDeleted),
                Deadline = h.Deadline
            })
            .ToListAsync();

        return Page();
    }

    public class GroupItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class HomeworkRow
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public DateTime LessonDate { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LessonTopic { get; set; } = string.Empty;
        public int SubmittedCount { get; set; }
        public int TotalStudents { get; set; }
        public DateTime? Deadline { get; set; }
    }
}
