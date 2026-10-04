using System.ComponentModel.DataAnnotations;
using Journal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Journal.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, isPersistent: false, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Неверный email или пароль");
            return Page();
        }

        // Редирект по роли
        var user = await _userManager.FindByEmailAsync(Input.Email);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

        if (roles.Contains("Admin"))
            return RedirectToPage("/Admin/Index");
        if (roles.Contains("Teacher"))
            return RedirectToPage("/Teacher/Index");
        return RedirectToPage("/Student/Index");
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите email")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
