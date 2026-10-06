using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class HomeworksModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public HomeworksModel(AppDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    public List<HomeworkRow> Items { get; set; } = new();
    public bool NoGroup { get; set; } = false;

    // new: скрывает PageModel.File(byte[], string) — здесь свойство, не метод
    [BindProperty]
    public new IFormFile? File { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        if (student.GroupId == null)
        {
            NoGroup = true;
            return Page();
        }

        var homeworks = await _db.Homeworks
            .Include(h => h.Lesson!).ThenInclude(l => l.Subject)
            .Where(h => h.Lesson!.GroupId == student.GroupId)
            .OrderByDescending(h => h.Lesson!.DateFrom)
            .ToListAsync();

        var mySubs = await _db.StudentHomeworks
            .Where(sh => sh.StudentId == student.Id)
            .ToListAsync();
        var mySubsByHw = mySubs.ToDictionary(sh => sh.HomeworkId);

        var myGrades = await _db.Grades
            .Where(g => g.StudentId == student.Id && g.Type == GradeType.Homework)
            .ToListAsync();
        var myGradesByLesson = myGrades.ToDictionary(g => g.LessonId, g => g.Value);

        Items = homeworks.Select(h => new HomeworkRow
        {
            Id = h.Id,
            LessonDate = h.Lesson!.DateFrom,
            SubjectName = h.Lesson.Subject?.Name ?? "",
            Topic = h.Lesson.Topic ?? "—",
            Description = h.Description ?? "—",
            FilePath = h.FilePath,
            Deadline = h.Deadline,
            MyFilePath = mySubsByHw.TryGetValue(h.Id, out var sub) ? sub.FilePath : null,
            MySubmittedAt = mySubsByHw.TryGetValue(h.Id, out var sub2) ? sub2.SubmittedAt : null,
            MyGrade = myGradesByLesson.TryGetValue(h.LessonId, out var gv) ? gv : null
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int homeworkId)
    {
        var student = await _userManager.GetUserAsync(User);
        if (student == null)
            return RedirectToPage("/Account/Login");

        if (student.GroupId == null)
            return NotFound();

        if (File == null || File.Length == 0)
        {
            TempData["Error"] = "Выберите файл";
            return RedirectToPage();
        }

        if (File.Length > 10 * 1024 * 1024)
        {
            TempData["Error"] = "Файл не больше 10 МБ";
            return RedirectToPage();
        }

        var hw = await _db.Homeworks
            .Include(h => h.Lesson)
            .FirstOrDefaultAsync(h => h.Id == homeworkId);
        if (hw == null || hw.Lesson == null || hw.Lesson.GroupId != student.GroupId)
            return NotFound();

        var dir = Path.Combine(_env.WebRootPath, "uploads", "homeworks", "students");
        Directory.CreateDirectory(dir);
        var ext = Path.GetExtension(File.FileName);
        var name = Guid.NewGuid().ToString() + ext;
        var fullPath = Path.Combine(dir, name);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await File.CopyToAsync(stream);
        }
        var relPath = "/uploads/homeworks/students/" + name;

        var existing = await _db.StudentHomeworks
            .FirstOrDefaultAsync(sh => sh.HomeworkId == homeworkId && sh.StudentId == student.Id);
        string? oldPath = null;
        if (existing != null)
        {
            oldPath = existing.FilePath;
            existing.FilePath = relPath;
            existing.SubmittedAt = DateTime.UtcNow;
        }
        else
        {
            _db.StudentHomeworks.Add(new StudentHomework
            {
                HomeworkId = homeworkId,
                StudentId = student.Id,
                FilePath = relPath,
                SubmittedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        // Старый файл удаляем после успешного сохранения в БД
        if (oldPath != null)
        {
            var oldFull = Path.Combine(_env.WebRootPath, oldPath.TrimStart('/'));
            if (System.IO.File.Exists(oldFull))
                System.IO.File.Delete(oldFull);
        }

        TempData["Success"] = "Работа загружена";
        return RedirectToPage();
    }

    public class HomeworkRow
    {
        public int Id { get; set; }
        public DateTime LessonDate { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public DateTime? Deadline { get; set; }
        public string? MyFilePath { get; set; }
        public DateTime? MySubmittedAt { get; set; }
        public int? MyGrade { get; set; }
    }
}
