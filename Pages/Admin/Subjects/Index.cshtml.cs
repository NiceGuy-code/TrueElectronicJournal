using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Subjects;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<SubjectRow> Items { get; set; } = new();

    // Один запрос: количество занятий и учителей считаются подзапросами COUNT
    public async Task OnGetAsync()
    {
        Items = await _db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SubjectRow
            {
                Id = s.Id,
                Name = s.Name,
                LessonsCount = s.Lessons.Count,
                TeachersCount = s.Lessons.Select(l => l.TeacherId).Distinct().Count()
            })
            .ToListAsync();
    }

    // Удаление: нельзя, если по предмету есть занятия
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (await _db.Lessons.AnyAsync(l => l.SubjectId == id))
        {
            TempData["Error"] = "Нельзя удалить предмет: есть занятия";
            return Redirect("/Admin/Subjects");
        }

        var subject = await _db.Subjects.FindAsync(id);
        if (subject != null)
        {
            _db.Subjects.Remove(subject);
            await _db.SaveChangesAsync();
        }
        return Redirect("/Admin/Subjects");
    }

    public class SubjectRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LessonsCount { get; set; }
        public int TeachersCount { get; set; }
    }
}
