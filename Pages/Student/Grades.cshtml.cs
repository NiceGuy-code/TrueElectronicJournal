using Journal.Data;
using Journal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Journal.Pages.Student;

[Authorize(Roles = "Student")]
public class GradesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GradesModel(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public bool NoGroup { get; set; } = false;

    [BindProperty(SupportsGet = true)]
    public string? Subject { get; set; }

    public List<SubjectSummary> Summary { get; set; } = new();
    public int OverallCount { get; set; }
    public double? OverallAverage { get; set; }

    public List<SubjectGradeRow> SubjectGrades { get; set; } = new();
    public int SubjectCount { get; set; }
    public double? SubjectAverage { get; set; }

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

        // Сводка: только числовые оценки (н/б имеет Value = null и не входит)
        var grouped = await _db.Grades
            .Where(g => g.StudentId == student.Id && g.Value != null)
            .GroupBy(g => g.Lesson!.Subject!.Name)
            .Select(grp => new SubjectSummary
            {
                SubjectName = grp.Key,
                Count = grp.Count(),
                Sum = grp.Sum(x => x.Value!.Value)
            })
            .ToListAsync();

        OverallCount = grouped.Sum(s => s.Count);
        OverallAverage = OverallCount > 0
            ? grouped.Sum(s => (double)s.Sum) / OverallCount
            : null;

        // Все предметы по алфавиту, включая те, где оценок нет (merge в памяти)
        var subjectNames = await _db.Subjects
            .OrderBy(s => s.Name)
            .Select(s => s.Name)
            .ToListAsync();
        var byName = grouped.ToDictionary(s => s.SubjectName);
        Summary = subjectNames
            .Select(name => byName.TryGetValue(name, out var s)
                ? s
                : new SubjectSummary { SubjectName = name })
            .ToList();

        // Детальный режим: оценки одного предмета
        if (Subject != null)
        {
            SubjectGrades = await _db.Grades
                .Where(g => g.StudentId == student.Id
                    && g.Value != null
                    && g.Lesson!.Subject!.Name == Subject)
                .OrderBy(g => g.Lesson!.DateFrom)
                .Select(g => new SubjectGradeRow
                {
                    Date = g.Lesson!.DateFrom,
                    Value = g.Value!.Value
                })
                .ToListAsync();

            SubjectCount = SubjectGrades.Count;
            SubjectAverage = SubjectCount > 0
                ? SubjectGrades.Sum(g => (double)g.Value) / SubjectCount
                : null;
        }

        return Page();
    }

    public class SubjectSummary
    {
        public string SubjectName { get; set; } = "";
        public int Count { get; set; }
        public int Sum { get; set; }
        public double Average => Count > 0 ? (double)Sum / Count : 0;
    }

    public class SubjectGradeRow
    {
        public DateTime Date { get; set; }
        public int Value { get; set; }
    }
}
