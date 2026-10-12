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
public class CreateRecurringModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateRecurringModel(AppDbContext db, UserManager<ApplicationUser> userManager)
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

        if (Input.DaysOfWeek.Count == 0)
            ModelState.AddModelError("Input.DaysOfWeek", "Выберите хотя бы один день недели");

        if (Input.DateTo < Input.DateFrom)
            ModelState.AddModelError("Input.DateTo", "Дата окончания должна быть позже");

        if ((Input.DateTo!.Value - Input.DateFrom!.Value).TotalDays > 180)
            ModelState.AddModelError("Input.DateTo", "Период не больше 180 дней");

        if (Input.TimeTo <= Input.TimeFrom)
            ModelState.AddModelError("Input.TimeTo", "Время окончания должно быть позже начала");

        var teacher = await _userManager.FindByIdAsync(Input.TeacherId);
        if (teacher == null || teacher.IsDeleted || !await _userManager.IsInRoleAsync(teacher, "Teacher"))
            ModelState.AddModelError("Input.TeacherId", "Учитель не найден");

        if (!await _db.Subjects.AnyAsync(s => s.Id == Input.SubjectId))
            ModelState.AddModelError("Input.SubjectId", "Предмет не найден");

        if (!await _db.Groups.AnyAsync(g => g.Id == Input.GroupId))
            ModelState.AddModelError("Input.GroupId", "Группа не найдена");

        if (!ModelState.IsValid)
            return Page();

        var startDate = Input.DateFrom!.Value.Date;
        var endDate = Input.DateTo!.Value.Date;
        var timeFrom = Input.TimeFrom!.Value;
        var timeTo = Input.TimeTo!.Value;

        // Существующие даты одним запросом — защита от дубликатов
        var existing = await _db.Lessons
            .Where(l => l.GroupId == Input.GroupId
                && l.DateFrom >= startDate && l.DateFrom <= endDate.AddDays(1))
            .Select(l => l.DateFrom)
            .ToListAsync();
        var existingSet = new HashSet<DateTime>(existing);

        int created = 0, skipped = 0;
        bool limited = false;

        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            // DayOfWeek: 0=Вс..6=Сб → наш формат 1=Пн..7=Вс
            int dow = (int)d.DayOfWeek == 0 ? 7 : (int)d.DayOfWeek;
            if (!Input.DaysOfWeek.Contains(dow))
                continue;

            var df = DateTime.SpecifyKind(d + timeFrom, DateTimeKind.Utc);
            var dt = DateTime.SpecifyKind(d + timeTo, DateTimeKind.Utc);

            if (existingSet.Contains(df))
            {
                skipped++;
                continue;
            }

            _db.Lessons.Add(new Lesson
            {
                SubjectId = Input.SubjectId!.Value,
                GroupId = Input.GroupId!.Value,
                TeacherId = Input.TeacherId,
                Topic = Input.Topic.Trim(),
                DateFrom = df,
                DateTo = dt
            });

            created++;
            if (created >= 100)
            {
                limited = true;
                break;
            }
        }

        await _db.SaveChangesAsync();

        var msg = $"Создано {created} занятий";
        if (skipped > 0)
            msg += $", пропущено {skipped} дубликатов";
        if (limited)
            msg += ". Достигнут лимит 100";

        TempData["Success"] = msg;
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

        [Required(ErrorMessage = "Введите тему")]
        [MaxLength(200)]
        public string Topic { get; set; } = string.Empty;

        public List<int> DaysOfWeek { get; set; } = new();

        [Required(ErrorMessage = "Укажите время начала")]
        [DataType(DataType.Time)]
        public TimeSpan? TimeFrom { get; set; }

        [Required(ErrorMessage = "Укажите время окончания")]
        [DataType(DataType.Time)]
        public TimeSpan? TimeTo { get; set; }

        [Required(ErrorMessage = "Укажите дату начала")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [Required(ErrorMessage = "Укажите дату окончания")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }
    }
}
