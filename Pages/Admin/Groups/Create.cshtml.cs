using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Groups;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var name = Input.Name.Trim();

        if (await _db.Groups.AnyAsync(g => g.Name == name))
        {
            ModelState.AddModelError("Input.Name", "Группа с таким названием уже есть");
            return Page();
        }

        _db.Groups.Add(new Group { Name = name });
        await _db.SaveChangesAsync();

        return Redirect("/Admin/Groups");
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите название")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
