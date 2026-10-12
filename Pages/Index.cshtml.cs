using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Journal.Pages;

// Корень "/" — редирект по роли (аноним → логин)
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToPage("/Account/Login");
        if (User.IsInRole("Admin"))
            return RedirectToPage("/Admin/Index");
        if (User.IsInRole("Teacher"))
            return RedirectToPage("/Teacher/Index");
        return RedirectToPage("/Student/Index");
    }
}
