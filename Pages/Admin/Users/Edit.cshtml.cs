using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Users;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    // Роли фиксированные, RoleManager не нужен
    private static readonly string[] Roles = { "Student", "Teacher", "Admin" };

    public EditModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> RoleItems { get; set; } = new();
    public List<SelectListItem> GroupItems { get; set; } = new();

    // Для формы: id живёт в маршруте, в InputModel его нет
    public string UserId { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        UserId = id;

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        Input = new InputModel
        {
            FullName = user.FullName,
            Email = user.Email ?? "",
            Role = roles.FirstOrDefault() ?? "Student",
            GroupId = user.GroupId
        };

        await FillListsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        // Списки нужны и при ошибке, чтобы dropdown'ы не опустели
        await FillListsAsync();

        UserId = id;

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        if (!Roles.Contains(Input.Role))
        {
            ModelState.AddModelError("Input.Role", "Выберите роль из списка");
            return Page();
        }

        if (Input.Role == "Student" && Input.GroupId == null)
        {
            ModelState.AddModelError("Input.GroupId", "Для студента укажите группу");
            return Page();
        }

        if (await _userManager.Users.AnyAsync(u => u.Email == Input.Email && u.Id != id))
        {
            ModelState.AddModelError("Input.Email", "Email уже занят");
            return Page();
        }

        // Пароль первым: при ошибке остальные правки не применяются
        if (!string.IsNullOrEmpty(Input.NewPassword))
        {
            // Удаляем старый пароль (если есть)
            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                foreach (var e in removeResult.Errors)
                    ModelState.AddModelError("", e.Description);
                return Page();
            }

            // Ставим новый
            var addResult = await _userManager.AddPasswordAsync(user, Input.NewPassword);
            if (!addResult.Succeeded)
            {
                foreach (var e in addResult.Errors)
                    ModelState.AddModelError("", e.Description);
                return Page();
            }
        }

        user.FullName = Input.FullName;
        user.Email = Input.Email;
        user.UserName = Input.Email;
        // Без этого FindByEmailAsync/FindByNameAsync перестанут находить юзера
        user.NormalizedEmail = _userManager.NormalizeEmail(Input.Email);
        user.NormalizedUserName = _userManager.NormalizeName(Input.Email);
        user.GroupId = Input.Role == "Student" ? Input.GroupId : null;

        // UpdateUserAsync у UserManager protected — сохраняем через общий контекст
        await _db.SaveChangesAsync();

        // Одна роль у пользователя: меняем только при отличии
        var curRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
        if (curRole != Input.Role)
        {
            if (curRole != null)
                await _userManager.RemoveFromRoleAsync(user, curRole);

            var add = await _userManager.AddToRoleAsync(user, Input.Role);
            if (!add.Succeeded)
            {
                foreach (var e in add.Errors)
                    ModelState.AddModelError("", e.Description);
                return Page();
            }
        }

        return Redirect("/Admin/Users");
    }

    private async Task FillListsAsync()
    {
        RoleItems = Roles.Select(r => new SelectListItem(r, r)).ToList();

        GroupItems = new List<SelectListItem> { new("— не указана —", "") };
        GroupItems.AddRange(await _db.Groups
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem(g.Name, g.Id.ToString()))
            .ToListAsync());
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите ФИО")]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите роль")]
        public string Role { get; set; } = string.Empty;

        public int? GroupId { get; set; }

        // Пусто = не менять; string?, иначе NRT даёт неявный Required
        public string? NewPassword { get; set; }
    }
}
