using System.ComponentModel.DataAnnotations;
using Journal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Journal.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (await _userManager.FindByEmailAsync(Input.Email) != null)
        {
            ModelState.AddModelError("Input.Email", "Email уже занят");
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FullName = Input.FullName
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return Page();
        }

        // Роль Student (роль создаётся в SeedData)
        var roleResult = await _userManager.AddToRoleAsync(user, "Student");
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
                ModelState.AddModelError("", error.Description);
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToPage("/Student/Index");
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
        [DataType(DataType.Password)]
        [MinLength(4, ErrorMessage = "Пароль от 4 символов")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторите пароль")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
