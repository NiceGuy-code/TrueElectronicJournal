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
    public int Subjects { get; set; }
    public int Lessons { get; set; }
    public int Announcements { get; set; }

    // Количество пользователей с ролью: один запрос с JOIN, без удалённых
    private Task<int> CountByRole(string role) =>
        (from ur in _db.UserRoles
         join r in _db.Roles on ur.RoleId equals r.Id
         join u in _db.Users on ur.UserId equals u.Id
         where r.Name == role && !u.IsDeleted
         select ur.UserId).CountAsync();

    public async Task OnGetAsync()
    {
        TotalUsers = await _db.Users.CountAsync(u => !u.IsDeleted);
        Students = await CountByRole("Student");
        Teachers = await CountByRole("Teacher");
        Admins = await CountByRole("Admin");
        Groups = await _db.Groups.CountAsync();
        Subjects = await _db.Subjects.CountAsync();
        Lessons = await _db.Lessons.CountAsync();
        Announcements = await _db.Announcements.CountAsync();
    }
}
