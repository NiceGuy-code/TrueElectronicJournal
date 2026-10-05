using System.ComponentModel.DataAnnotations;
using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Admin.Schedule;

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

    public List<SelectListItem> SubjectItems { get; set; } = new();
    public List<SelectListItem> GroupItems { get; set; } = new();
    public List<SelectListItem> TeacherItems { get; set; } = new();

    public async Task OnGetAsync()
    {
        await FillItemsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Списки нужны и при ошибке, чтобы select'ы не опустели
        await FillItemsAsync();

        if (!ModelState.IsValid)
            return Page();

        // Nullable: иначе [Required] для значимых типов не срабатывает
        // и пустой select/time даёт английскую ошибку биндинга.
        // После проверки ModelState значения не null — .Value падает громко, если что-то не так
        var dateFrom = Input.Date!.Value.Date + Input.TimeFrom!.Value;
        var dateTo = Input.Date!.Value.Date + Input.TimeTo!.Value;

        if (dateTo <= dateFrom)
        {
            ModelState.AddModelError("Input.TimeTo", "Время окончания должно быть позже начала");
            return Page();
        }

        var teacher = await _userManager.FindByIdAsync(Input.TeacherId);
        if (teacher == null || teacher.IsDeleted || !await _userManager.IsInRoleAsync(teacher, "Teacher"))
        {
            ModelState.AddModelError("Input.TeacherId", "Учитель не найден");
            return Page();
        }

        if (!await _db.Subjects.AnyAsync(s => s.Id == Input.SubjectId))
        {
            ModelState.AddModelError("Input.SubjectId", "Предмет не найден");
            return Page();
        }

        if (!await _db.Groups.AnyAsync(g => g.Id == Input.GroupId))
        {
            ModelState.AddModelError("Input.GroupId", "Группа не найдена");
            return Page();
        }

        _db.Lessons.Add(new Lesson
        {
            SubjectId = Input.SubjectId!.Value,
            GroupId = Input.GroupId!.Value,
            TeacherId = Input.TeacherId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Topic = Input.Topic.Trim()
        });
        await _db.SaveChangesAsync();

        return Redirect("/Admin/Schedule");
    }

    private async Task FillItemsAsync()
    {
        SubjectItems = await _db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem(s.Name, s.Id.ToString()))
            .ToListAsync();

        GroupItems = await _db.Groups
            .OrderBy(g => g.Name)
            .Select(g => new SelectListItem(g.Name, g.Id.ToString()))
            .ToListAsync();

        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        TeacherItems = teachers
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.FullName)
            .Select(t => new SelectListItem(t.FullName, t.Id))
            .ToList();
    }

    public class InputModel
    {
        [Required(ErrorMessage = "Выберите предмет")]
        public int? SubjectId { get; set; }

        [Required(ErrorMessage = "Выберите группу")]
        public int? GroupId { get; set; }

        [Required(ErrorMessage = "Выберите учителя")]
        public string TeacherId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите дату")]
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Укажите время начала")]
        [DataType(DataType.Time)]
        public TimeSpan? TimeFrom { get; set; }

        [Required(ErrorMessage = "Укажите время окончания")]
        [DataType(DataType.Time)]
        public TimeSpan? TimeTo { get; set; }

        [Required(ErrorMessage = "Введите тему занятия")]
        [MaxLength(200)]
        public string Topic { get; set; } = string.Empty;
    }
}
