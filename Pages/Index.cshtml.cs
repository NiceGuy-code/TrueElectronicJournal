using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Journal.Pages;

// Корень "/" — аноним видит герой-экран, авторизованные идут по роли
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Page();
        if (User.IsInRole("Admin"))
            return RedirectToPage("/Admin/Index");
        if (User.IsInRole("Teacher"))
            return RedirectToPage("/Teacher/Index");
        return RedirectToPage("/Student/Index");
    }
}
