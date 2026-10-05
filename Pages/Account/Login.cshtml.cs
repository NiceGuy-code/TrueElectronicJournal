using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            ModelState.AddModelError("", "Неверный email или пароль");
            return Page();
        }

        var check = await _signInManager.CheckPasswordSignInAsync(
            user, Input.Password, lockoutOnFailure: false);

        if (!check.Succeeded)
        {
            ModelState.AddModelError("", "Неверный email или пароль");
            return Page();
        }

        // Кука с claim ФИО для навбара
        await _signInManager.SignInWithClaimsAsync(user, isPersistent: false,
            new[] { new Claim("FullName", user.FullName) });

        // Возврат на страницу, с которой пришли на вход
        var returnUrl = Request.Query["ReturnUrl"].ToString();
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        // Редирект по роли
        var roles = await _userManager.GetRolesAsync(user);

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
