using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<UserRow> Rows { get; set; } = new();

    // Один запрос с левыми JOIN — без цикла и без GetRolesAsync
    public async Task OnGetAsync()
    {
        Rows = await (
            from u in _db.Users
            from ur in _db.UserRoles.Where(x => x.UserId == u.Id).DefaultIfEmpty()
            from r in _db.Roles.Where(x => x.Id == ur.RoleId).DefaultIfEmpty()
            from g in _db.Groups.Where(x => x.Id == u.GroupId).DefaultIfEmpty()
            orderby u.FullName
            select new UserRow
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? "",
                Role = r == null ? "" : r.Name ?? "",
                GroupName = g == null ? "" : g.Name
            }).ToListAsync();
    }

    public class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
    }
}
