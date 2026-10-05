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
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    // Роли фиксированные, RoleManager не нужен
    private static readonly string[] Roles = { "Student", "Teacher", "Admin" };

    public CreateModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> RoleItems { get; set; } = new();
    public List<SelectListItem> GroupItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        await FillListsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Списки нужны и при ошибке, чтобы dropdown'ы не опустели
        await FillListsAsync();

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

        if (await _userManager.Users.AnyAsync(u => u.Email == Input.Email))
        {
            ModelState.AddModelError("Input.Email", "Email уже занят");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true,
            FullName = Input.FullName,
            GroupId = Input.Role == "Student" ? Input.GroupId : null
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return Page();
        }

        result = await _userManager.AddToRoleAsync(user, Input.Role);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Description);
            return Page();
        }

        // Маршрут Index задан в @page, поэтому путь напрямую
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

        [Required(ErrorMessage = "Введите пароль")]
        [MinLength(4, ErrorMessage = "Пароль от 4 символов")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите роль")]
        public string Role { get; set; } = string.Empty;

        public int? GroupId { get; set; }
    }
}
