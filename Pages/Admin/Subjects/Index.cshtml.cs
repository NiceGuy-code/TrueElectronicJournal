using Journal.Data;
using Microsoft.AspNetCore.Authorization;
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

    // Один запрос: только список предметов
    public async Task OnGetAsync()
    {
        Items = await _db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SubjectRow { Id = s.Id, Name = s.Name })
            .ToListAsync();
    }

    public class SubjectRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
