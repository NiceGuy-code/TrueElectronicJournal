using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Groups;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<GroupRow> Rows { get; set; } = new();

    // Один запрос: количество студентов считается подзапросом COUNT
    public async Task OnGetAsync()
    {
        Rows = await _db.Groups
            .OrderBy(g => g.Name)
            .Select(g => new GroupRow
            {
                Id = g.Id,
                Name = g.Name,
                StudentsCount = g.Students.Count(s => !s.IsDeleted),
                LessonsCount = g.Lessons.Count,
                LastLessonDate = g.Lessons.Any() ? g.Lessons.Max(l => l.DateFrom) : (DateTime?)null
            })
            .ToListAsync();
    }

    // Удаление: POST-форма из строки таблицы
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var group = await _db.Groups.FirstOrDefaultAsync(g => g.Id == id);
        if (group == null)
            return Redirect("/Admin/Groups");

        // Проверяем отдельными запросами: навигации Students/Lessons не загружены
        var hasStudents = await _db.Users.AnyAsync(u => u.GroupId == id);
        var hasLessons = await _db.Lessons.AnyAsync(l => l.GroupId == id);

        if (hasStudents || hasLessons)
        {
            TempData["Error"] = "Нельзя удалить группу: в ней есть студенты или занятия";
            return Redirect("/Admin/Groups");
        }

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync();
        return Redirect("/Admin/Groups");
    }

    public class GroupRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StudentsCount { get; set; }
        public int LessonsCount { get; set; }
        public DateTime? LastLessonDate { get; set; }
    }
}
