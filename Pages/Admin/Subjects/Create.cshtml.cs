using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Subjects;

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

        if (await _db.Subjects.AnyAsync(s => s.Name == name))
        {
            ModelState.AddModelError("Input.Name", "Предмет с таким названием уже есть");
            return Page();
        }

        _db.Subjects.Add(new Subject { Name = name });
        await _db.SaveChangesAsync();

        return Redirect("/Admin/Subjects");
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите название")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
