using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Subjects;

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
        var s = await _db.Subjects.FindAsync(id);
        if (s == null)
            return NotFound();

        Input = new InputModel { Name = s.Name };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var s = await _db.Subjects.FindAsync(id);
        if (s == null)
            return NotFound();

        if (!ModelState.IsValid)
            return Page();

        var name = Input.Name.Trim();

        if (await _db.Subjects.AnyAsync(x => x.Name == name && x.Id != id))
        {
            ModelState.AddModelError("Input.Name", "Предмет с таким названием уже есть");
            return Page();
        }

        s.Name = name;
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
