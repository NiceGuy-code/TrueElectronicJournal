using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Groups;

[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var group = await _db.Groups.FindAsync(id);
        if (group == null)
            return NotFound();

        Input = new InputModel { Name = group.Name };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var group = await _db.Groups.FindAsync(id);
        if (group == null)
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        var name = Input.Name.Trim();

        if (await _db.Groups.AnyAsync(g => g.Name == name && g.Id != id))
        {
            ModelState.AddModelError("Input.Name", "Группа с таким названием уже есть");
            return Page();
        }

        group.Name = name;
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
