using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Announcements;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> GroupItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        await FillGroupItemsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Список нужен и при ошибке, чтобы select не опустел
        await FillGroupItemsAsync();

        if (!ModelState.IsValid)
            return Page();

        var admin = await _userManager.GetUserAsync(User);
        if (admin == null)
            return Redirect("/Account/Login");

        var ann = new Announcement
        {
            Title = Input.Title.Trim(),
            Text = Input.Text.Trim(),
            CreatedAt = DateTime.UtcNow,
            AuthorId = admin.Id,
            GroupId = Input.GroupId
        };

        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        return Redirect("/Admin/Announcements");
    }

    private async Task FillGroupItemsAsync()
    {
        GroupItems = new List<SelectListItem> { new("Для всех", "") };
        GroupItems.AddRange(await _db.Groups
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem(g.Name, g.Id.ToString()))
            .ToListAsync());
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите заголовок")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите текст")]
        [MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        public int? GroupId { get; set; }
    }
}
