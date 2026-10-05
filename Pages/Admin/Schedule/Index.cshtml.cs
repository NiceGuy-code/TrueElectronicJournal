using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Schedule;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<LessonRow> Items { get; set; } = new();

    // Чистая проекция: Include в паре с Select всё равно игнорируется
    public async Task OnGetAsync()
    {
        Items = await _db.Lessons
            .OrderByDescending(l => l.DateFrom)
            .Select(l => new LessonRow
            {
                Id = l.Id,
                DateFrom = l.DateFrom,
                DateTo = l.DateTo,
                SubjectName = l.Subject == null ? "" : l.Subject.Name,
                GroupName = l.Group == null ? "" : l.Group.Name,
                TeacherName = l.Teacher.FullName,
                Topic = l.Topic
            })
            .ToListAsync();
    }

    public class LessonRow
    {
        public int Id { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string? Topic { get; set; }
    }
}
