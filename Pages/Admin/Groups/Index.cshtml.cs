using Journal.Data;
using Microsoft.AspNetCore.Authorization;
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
            .Select(g => new GroupRow
            {
                Id = g.Id,
                Name = g.Name,
                StudentsCount = g.Students.Count
            })
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public class GroupRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StudentsCount { get; set; }
    }
}
