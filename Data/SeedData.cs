using Journal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Journal.Data;

// Роли, админ и тестовые данные создаются при старте, миграцией не создаются
public static class SeedData
{
    private static readonly string[] SubjectNames =
    {
        "Математика", "Физика", "Русский язык", "Литература", "Информатика", "История"
    };

    // Предмет → закреплённый учитель
    private static readonly Dictionary<string, string> SubjectTeachers = new()
    {
        ["Математика"] = "teacher1@test.ru",
        ["Физика"] = "teacher1@test.ru",
        ["Русский язык"] = "teacher2@test.ru",
        ["Литература"] = "teacher2@test.ru",
        ["Информатика"] = "teacher3@test.ru",
        ["История"] = "teacher3@test.ru"
    };

    // Предмет → темы занятий
    private static readonly Dictionary<string, string[]> SubjectTopics = new()
    {
        ["Математика"] = new[] { "Квадратные уравнения", "Теорема Пифагора", "Графики функций" },
        ["Физика"] = new[] { "Законы Ньютона", "Сила и давление", "Электрический ток" },
        ["Русский язык"] = new[] { "Орфография", "Сложносоставные предложения", "Односоставные предложения" },
        ["Литература"] = new[] { "Пушкин, «Евгений Онегин»", "Толстой, «Война и мир»", "Русская басня" },
        ["Информатика"] = new[] { "Алгоритмы и диаграммы", "Системы счисления", "Табличный процессор" },
        ["История"] = new[] { "Древний мир", "Средние века", "Россия в XIX веке" }
    };

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        // 1. Роли
        foreach (var role in new[] { "Admin", "Teacher", "Student" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Админ
        var adminEmail = "admin@test.ru";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Администратор"
            };
            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
            else
                logger.LogError("Не удалось создать админа: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // 3-4. Первый учитель и студент (группа привяжется в SeedUsersAsync)
        await CreateUserAsync(userManager, logger, "teacher1@test.ru", "Teacher123!",
            "Иванов Иван Иванович", "Teacher", null);
        await CreateUserAsync(userManager, logger, "student1@test.ru", "Student123!",
            "Петров Пётр Петрович", "Student", null);

        // 5-11. Демо-данные: ошибка одного шага не валит приложение
        async Task Run(string step, Func<Task> seed)
        {
            try
            {
                await seed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка seed на шаге {Step}", step);
            }
        }

        await Run("Groups", () => SeedGroupsAsync(services));
        await Run("Subjects", () => SeedSubjectsAsync(services));
        await Run("Users", () => SeedUsersAsync(services));
        await Run("Lessons", () => SeedLessonsAsync(services));
        await Run("Grades", () => SeedGradesAsync(services));
        await Run("Announcements", () => SeedAnnouncementsAsync(services));
        await Run("Homeworks", () => SeedHomeworksAsync(services));
    }

    private static async Task SeedGroupsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var name in new[] { "ИС-21", "ИС-22", "ИС-23", "ПИ-21" })
            if (!await db.Groups.AnyAsync(g => g.Name == name))
                db.Groups.Add(new Group { Name = name });

        await db.SaveChangesAsync();
    }

    private static async Task SeedSubjectsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var name in SubjectNames)
            if (!await db.Subjects.AnyAsync(s => s.Name == name))
                db.Subjects.Add(new Subject { Name = name });

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        // student1 → ИС-22, если группа ещё не выбрана
        var student1 = await userManager.FindByEmailAsync("student1@test.ru");
        if (student1 != null && student1.GroupId == null)
        {
            var is22 = await db.Groups.FirstOrDefaultAsync(g => g.Name == "ИС-22");
            if (is22 != null)
            {
                student1.GroupId = is22.Id;
                var upd = await userManager.UpdateAsync(student1);
                if (!upd.Succeeded)
                    logger.LogError("Не удалось привязать student1 к ИС-22: {Errors}",
                        string.Join("; ", upd.Errors.Select(e => e.Description)));
            }
        }

        await CreateUserAsync(userManager, logger, "teacher2@test.ru", "Teacher123!",
            "Петрова Мария Сергеевна", "Teacher", null);
        await CreateUserAsync(userManager, logger, "teacher3@test.ru", "Teacher123!",
            "Сидоров Алексей Петрович", "Teacher", null);

        var groups = await db.Groups.ToDictionaryAsync(g => g.Name, g => g.Id);

        var students = new (string Email, string FullName, string Group)[]
        {
            ("student2@test.ru", "Кузнецов Дмитрий Алексеевич", "ИС-21"),
            ("student3@test.ru", "Смирнов Артём Игоревич", "ИС-21"),
            ("student4@test.ru", "Новиков Максим Дмитриевич", "ИС-22"),
            ("student5@test.ru", "Морозов Кирилл Сергеевич", "ИС-22"),
            ("student6@test.ru", "Волков Илья Андреевич", "ИС-22"),
            ("student7@test.ru", "Соловьёв Роман Павлович", "ИС-23"),
            ("student8@test.ru", "Васильев Никита Владимирович", "ИС-23"),
            ("student9@test.ru", "Зайцев Егор Александрович", "ИС-23"),
            ("student10@test.ru", "Иванов Владислав Юрьевич", "ПИ-21"),
            ("student11@test.ru", "Петров Матвей Романович", "ПИ-21"),
            ("student12@test.ru", "Сидоров Антон Викторович", "ПИ-21")
        };

        foreach (var s in students)
            await CreateUserAsync(userManager, logger, s.Email, "Student123!",
                s.FullName, "Student", groups.GetValueOrDefault(s.Group));

        // В ИС-21 должно быть не меньше 3 студентов
        var is21 = await db.Groups.FirstOrDefaultAsync(g => g.Name == "ИС-21");
        if (is21 != null)
        {
            var count = await db.Users.CountAsync(u => u.GroupId == is21.Id && !u.IsDeleted);
            if (count < 3 && await userManager.FindByEmailAsync("student13@test.ru") == null)
                await CreateUserAsync(userManager, logger, "student13@test.ru", "Student123!",
                    "Фомин Данила Сергеевич", "Student", is21.Id);
        }
    }

    private static async Task SeedLessonsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        var today = DateTime.Now.Date;

        // Слоты Пн/Ср/Пт в периоде -21..+14 дней
        var slots = new List<DateTime>();
        for (var d = today.AddDays(-21); d <= today.AddDays(14); d = d.AddDays(1))
            if (d.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday)
                slots.Add(d);

        // На группу: 6 последних прошедших + 3 ближайших будущих
        var slotsForGroup = slots.Where(d => d < today).TakeLast(6)
            .Concat(slots.Where(d => d >= today).Take(3))
            .ToList();

        var seedGroups = new[] { "ИС-21", "ИС-22", "ИС-23", "ПИ-21" };
        var groups = await db.Groups
            .Where(g => seedGroups.Contains(g.Name))
            .OrderBy(g => g.Name)
            .ToListAsync();

        var subjectIds = await db.Subjects.ToDictionaryAsync(s => s.Name, s => s.Id);

        var teacherEmails = SubjectTeachers.Values.Distinct().ToList();
        var teacherIds = await db.Users
            .Where(u => u.Email != null && teacherEmails.Contains(u.Email))
            .ToDictionaryAsync(u => u.Email!, u => u.Id);

        var added = 0;
        foreach (var (group, gi) in groups.Select((g, i) => (g, i)))
        {
            // Идемпотентность: какие даты у группы уже есть
            var existing = (await db.Lessons
                .Where(l => l.GroupId == group.Id)
                .Select(l => l.DateFrom)
                .ToListAsync()).ToHashSet();

            for (var j = 0; j < slotsForGroup.Count; j++)
            {
                var subject = SubjectNames[(gi + j) % SubjectNames.Length];

                // Чётное занятие — 10:00-11:30, нечётное — 12:00-13:30
                var timeFrom = j % 2 == 0 ? new TimeSpan(10, 0, 0) : new TimeSpan(12, 0, 0);
                var timeTo = j % 2 == 0 ? new TimeSpan(11, 30, 0) : new TimeSpan(13, 30, 0);
                var dateFrom = slotsForGroup[j] + timeFrom;

                if (existing.Contains(dateFrom))
                    continue;
                if (!subjectIds.TryGetValue(subject, out var subjectId))
                    continue;
                if (!teacherIds.TryGetValue(SubjectTeachers[subject], out var teacherId))
                    continue;

                db.Lessons.Add(new Lesson
                {
                    SubjectId = subjectId,
                    GroupId = group.Id,
                    TeacherId = teacherId,
                    DateFrom = dateFrom,
                    DateTo = slotsForGroup[j] + timeTo,
                    Topic = SubjectTopics[subject][j % SubjectTopics[subject].Length]
                });
                added++;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seed: добавлено занятий — {Count}", added);
    }

    private static async Task SeedGradesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        var rng = new Random(42);

        var pastLessons = await db.Lessons
            .Where(l => l.DateFrom < DateTime.Now)
            .OrderBy(l => l.DateFrom)
            .ToListAsync();
        if (pastLessons.Count == 0)
            return;

        // Студенты по группам
        var studentsByGroup = (await db.Users
                .Where(u => u.GroupId != null && !u.IsDeleted)
                .Select(u => new { u.Id, u.GroupId })
                .ToListAsync())
            .GroupBy(u => u.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(u => u.Id).ToList());

        // Идемпотентность: занятые ячейки (student, lesson) для уроков/н-б
        var lessonIds = pastLessons.Select(l => l.Id).ToList();
        var taken = (await db.Grades
                .Where(g => lessonIds.Contains(g.LessonId)
                    && (g.Type == GradeType.Lesson || g.Type == GradeType.NA))
                .Select(g => new { g.LessonId, g.StudentId })
                .ToListAsync())
            .ToHashSet();

        var added = 0;
        foreach (var lesson in pastLessons)
        {
            if (!studentsByGroup.TryGetValue(lesson.GroupId, out var groupStudents))
                continue;

            foreach (var sid in groupStudents)
            {
                if (taken.Contains(new { LessonId = lesson.Id, StudentId = sid }))
                    continue;

                var roll = rng.Next(100);
                if (roll < 10)
                {
                    // Неаттестация
                    db.Grades.Add(new Grade
                    {
                        LessonId = lesson.Id,
                        StudentId = sid,
                        Type = GradeType.NA,
                        Value = null,
                        Attendance = null
                    });
                }
                else
                {
                    // Оценка: 5 (25%), 4 (40%), 3 (25%), 2 (10%)
                    var v = rng.Next(100);
                    var value = v < 25 ? 5 : v < 65 ? 4 : v < 90 ? 3 : 2;

                    // Посещаемость: Present 60, Excused 10, Unexcused 10, Late 10
                    var a = rng.Next(90);
                    var att = a < 60 ? AttendanceType.Present
                            : a < 70 ? AttendanceType.Excused
                            : a < 80 ? AttendanceType.Unexcused
                            : AttendanceType.Late;

                    db.Grades.Add(new Grade
                    {
                        LessonId = lesson.Id,
                        StudentId = sid,
                        Type = GradeType.Lesson,
                        Value = value,
                        Attendance = att
                    });
                }
                added++;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seed: добавлено оценок за занятия — {Count}", added);
    }

    private static async Task SeedAnnouncementsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@test.ru");
        if (admin == null)
            return;

        var groups = await db.Groups.ToDictionaryAsync(g => g.Name, g => g.Id);

        var items = new (string Title, string Text, string? Group)[]
        {
            ("Расписание на неделю",
                "Расписание на неделю: занятия по понедельникам, средам и пятницам.", null),
            ("Родительское собрание",
                "Родительское собрание состоится в субботу в 11:00. Явка обязательна.", null),
            ("Олимпиада по математике",
                "Регистрация на олимпиаду по математике открыта до конца недели.", null),
            ("Контрольная работа по математике",
                "В ближайшее время контрольная работа. Подготовьтесь по пройденным темам.", "ИС-22"),
            ("Контрольная работа по русскому языку",
                "Контрольная работа по орфографии. Повторите правила написания Н и НН.", "ИС-22"),
            ("Контрольная работа по физике",
                "Контрольная работа по теме «Законы Ньютона».", "ИС-23"),
            ("Контрольная работа по информатике",
                "Контрольная работа по теме «Алгоритмы и диаграммы».", "ИС-21"),
            ("Контрольная работа по истории",
                "Контрольная работа по теме «Средние века».", "ПИ-21")
        };

        var added = 0;
        for (var i = 0; i < items.Length; i++)
        {
            var (title, text, groupName) = items[i];
            if (await db.Announcements.AnyAsync(a => a.Title == title))
                continue;

            int? groupId = null;
            if (groupName != null && groups.TryGetValue(groupName, out var gid))
                groupId = gid;

            db.Announcements.Add(new Announcement
            {
                Title = title,
                Text = text,
                AuthorId = admin.Id,
                GroupId = groupId,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
            added++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seed: добавлено объявлений — {Count}", added);
    }

    private static async Task SeedHomeworksAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

        var rng = new Random(7);

        // 6 ДЗ: по 3 на группы ИС-22 и ИС-23 (свежайшие занятия)
        var hwByGroup = new Dictionary<string, List<Homework>>();
        var addedHomeworks = 0;

        foreach (var groupName in new[] { "ИС-22", "ИС-23" })
        {
            var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
            if (group == null)
                continue;

            var lessons = await db.Lessons
                .Where(l => l.GroupId == group.Id)
                .OrderByDescending(l => l.DateFrom)
                .Take(3)
                .ToListAsync();

            var list = new List<Homework>();
            var n = 3;
            foreach (var lesson in lessons)
            {
                var hw = await db.Homeworks.FirstOrDefaultAsync(h => h.LessonId == lesson.Id);
                if (hw == null)
                {
                    hw = new Homework
                    {
                        LessonId = lesson.Id,
                        Description = $"Параграф {n}, упражнения {5 + (n - 3) * 10}-{12 + (n - 3) * 10}",
                        Deadline = lesson.DateFrom.AddDays(7),
                        FilePath = null
                    };
                    db.Homeworks.Add(hw);
                    addedHomeworks++;
                }
                list.Add(hw);
                n++;
            }
            hwByGroup[groupName] = list.OrderBy(h => h.Lesson!.DateFrom).ToList();
        }

        await db.SaveChangesAsync();

        // Сдачи: ИС-22 — 4 студента × 2 ДЗ, ИС-23 — 3 студента × 1 ДЗ
        var addedSubmissions = 0;
        var addedHwGrades = 0;

        foreach (var (groupName, submitCount) in new[] { ("ИС-22", 2), ("ИС-23", 1) })
        {
            if (!hwByGroup.TryGetValue(groupName, out var hws) || hws.Count == 0)
                continue;

            var group = await db.Groups.FirstOrDefaultAsync(g => g.Name == groupName);
            if (group == null)
                continue;

            var students = await db.Users
                .Where(u => u.GroupId == group.Id && !u.IsDeleted)
                .OrderBy(u => u.Email)
                .Select(u => u.Id)
                .ToListAsync();

            foreach (var sid in students)
            {
                foreach (var hw in hws.Take(submitCount))
                {
                    // Сдача
                    if (!await db.StudentHomeworks.AnyAsync(s =>
                            s.HomeworkId == hw.Id && s.StudentId == sid))
                    {
                        db.StudentHomeworks.Add(new StudentHomework
                        {
                            HomeworkId = hw.Id,
                            StudentId = sid,
                            FilePath = null,
                            SubmittedAt = DateTime.UtcNow.AddDays(-rng.Next(0, 5))
                        });
                        addedSubmissions++;
                    }

                    // Оценка за ДЗ (Type = Homework)
                    if (!await db.Grades.AnyAsync(g =>
                            g.StudentId == sid
                            && g.LessonId == hw.LessonId
                            && g.Type == GradeType.Homework))
                    {
                        db.Grades.Add(new Grade
                        {
                            StudentId = sid,
                            LessonId = hw.LessonId,
                            Type = GradeType.Homework,
                            Value = rng.Next(2) == 0 ? 4 : 5
                        });
                        addedHwGrades++;
                    }
                }
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seed: ДЗ — {Hw}, сдач — {Subs}, оценок за ДЗ — {Grades}",
            addedHomeworks, addedSubmissions, addedHwGrades);
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager,
        ILogger logger, string email, string password, string fullName, string role, int? groupId)
    {
        if (await userManager.FindByEmailAsync(email) != null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            GroupId = role == "Student" ? groupId : null
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
        else
            logger.LogError("Не удалось создать {Email}: {Errors}", email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
