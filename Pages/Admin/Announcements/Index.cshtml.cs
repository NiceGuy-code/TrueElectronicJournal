using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Announcements;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Announcement> Items { get; set; } = new();

    // Автор и группа подгружаются сразу — без цикла и без N+1
    public async Task OnGetAsync()
    {
        Items = await _db.Announcements
            .Include(a => a.Author)
            .Include(a => a.Group)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }
}
