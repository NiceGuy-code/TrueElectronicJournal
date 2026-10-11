using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<UserRow> Rows { get; set; } = new();

    public string? SelectedRole { get; set; }
    public int? SelectedGroupId { get; set; }
    public List<Group> Groups { get; set; } = new();
    public List<string> Roles { get; set; } = new() { "Student", "Teacher", "Admin" };
    public bool HasFilter => !string.IsNullOrEmpty(SelectedRole) || SelectedGroupId.HasValue;

    // Один запрос с левыми JOIN — без цикла и без GetRolesAsync
    public async Task OnGetAsync(string? SelectedRole, int? SelectedGroupId)
    {
        this.SelectedRole = SelectedRole;
        this.SelectedGroupId = SelectedGroupId;
        Groups = await _db.Groups.OrderBy(g => g.Name).ToListAsync();

        var query = from u in _db.Users
                    from ur in _db.UserRoles.Where(x => x.UserId == u.Id).DefaultIfEmpty()
                    from r in _db.Roles.Where(x => x.Id == ur.RoleId).DefaultIfEmpty()
                    from g in _db.Groups.Where(x => x.Id == u.GroupId).DefaultIfEmpty()
                    where !u.IsDeleted
                    select new { u, r, g };

        if (!string.IsNullOrEmpty(SelectedRole))
            query = query.Where(x => x.r!.Name == SelectedRole);
        if (SelectedGroupId.HasValue)
            query = query.Where(x => x.u.GroupId == SelectedGroupId.Value);

        Rows = await query.OrderBy(x => x.u.FullName)
            .Select(x => new UserRow
            {
                Id = x.u.Id,
                FullName = x.u.FullName,
                Email = x.u.Email!,
                Role = x.r != null ? x.r.Name! : "—",
                GroupName = x.g != null ? x.g.Name : null
            }).ToListAsync();
    }

    // Удаление: POST-форма из строки таблицы
    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var me = await _userManager.GetUserAsync(User);
        if (me?.Id == id)
        {
            TempData["Error"] = "Нельзя удалить самого себя";
            return Redirect("/Admin/Users");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return Redirect("/Admin/Users");

        // Мягкое удаление: роли снимаем, данные (занятия, оценки) сохраняем
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count > 0)
            await _userManager.RemoveFromRolesAsync(user, roles);

        user.IsDeleted = true;
        await _db.SaveChangesAsync();

        return Redirect("/Admin/Users");
    }

    public class UserRow
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? GroupName { get; set; }
    }
}
