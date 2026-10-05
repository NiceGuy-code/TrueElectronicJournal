using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public int TotalUsers { get; set; }
    public int Students { get; set; }
    public int Teachers { get; set; }
    public int Admins { get; set; }
    public int Groups { get; set; }
    public int Announcements { get; set; }

    // Количество пользователей с ролью: один запрос с JOIN
    private Task<int> CountByRole(string role) =>
        (from ur in _db.UserRoles
         join r in _db.Roles on ur.RoleId equals r.Id
         where r.Name == role
         select ur.UserId).CountAsync();

    public async Task OnGetAsync()
    {
        TotalUsers = await _db.Users.CountAsync();
        Students = await CountByRole("Student");
        Teachers = await CountByRole("Teacher");
        Admins = await CountByRole("Admin");
        Groups = await _db.Groups.CountAsync();
        Announcements = await _db.Announcements.CountAsync();
    }
}
