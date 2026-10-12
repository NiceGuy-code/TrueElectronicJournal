using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Teacher.Homeworks;

[Authorize(Roles = "Teacher")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public CreateModel(AppDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> LessonItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return;

        await FillLessonsAsync(teacher.Id);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var teacher = await _userManager.GetUserAsync(User);
        if (teacher == null)
            return RedirectToPage("/Account/Login");

        // Списки нужны и при ошибке, чтобы select не опустел
        await FillLessonsAsync(teacher.Id);

        if (!ModelState.IsValid)
            return Page();

        if (Input.Deadline.HasValue && Input.Deadline.Value.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("Input.Deadline", "Срок сдачи не может быть в прошлом");
            return Page();
        }

        var lesson = await _db.Lessons.FindAsync(Input.LessonId);
        if (lesson == null || lesson.TeacherId != teacher.Id)
        {
            ModelState.AddModelError("Input.LessonId", "Занятие не найдено");
            return Page();
        }

        string? filePath = null;
        if (Input.File != null)
        {
            var ext = Path.GetExtension(Input.File.FileName);
            var name = Guid.NewGuid().ToString() + ext;
            var dir = Path.Combine(_env.WebRootPath, "uploads", "homeworks");
            Directory.CreateDirectory(dir);
            using (var stream = new FileStream(Path.Combine(dir, name), FileMode.Create))
            {
                await Input.File.CopyToAsync(stream);
            }
            filePath = "/uploads/homeworks/" + name;
        }

        _db.Homeworks.Add(new Homework
        {
            LessonId = lesson.Id,
            Description = Input.Description.Trim(),
            FilePath = filePath,
            Deadline = Input.Deadline
        });
        await _db.SaveChangesAsync();

        return Redirect("/Teacher/Homeworks");
    }

    private async Task FillLessonsAsync(string teacherId)
    {
        var data = await _db.Lessons
            .Where(l => l.TeacherId == teacherId)
            .Select(l => new { l.Id, GroupName = l.Group!.Name, SubjectName = l.Subject!.Name, l.DateFrom })
            .OrderBy(x => x.GroupName)
            .ThenBy(x => x.DateFrom)
            .ToListAsync();

        LessonItems = data.Select(x => new SelectListItem(
            $"[{x.GroupName}] {x.DateFrom:dd.MM.yyyy} — {x.SubjectName}",
            x.Id.ToString()
        )).ToList();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Выберите занятие")]
        public int? LessonId { get; set; }

        [Required(ErrorMessage = "Введите описание задания")]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime? Deadline { get; set; }

        public IFormFile? File { get; set; }
    }
}
