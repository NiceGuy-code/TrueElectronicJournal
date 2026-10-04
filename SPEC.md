# Электронный журнал — курсовая (упрощённая версия)

## Стек
- ASP.NET Core 8, Razor Pages
- EF Core + SQL Server (LocalDB), строка подключения в appsettings.json
- ASP.NET Core Identity для логина. Пароль БЕЗ хэширования (учебный проект) — заглушка PasswordHasher.
- Bootstrap 5, минимум JS

## Роли
- Student (по умолчанию при регистрации)
- Teacher
- Admin

## Сущности (минимум)
- ApplicationUser : IdentityUser + FullName, GroupId (nullable)
- Group : Id, Name
- Subject : Id, Name, TeacherId
- Lesson : Id, SubjectId, GroupId, Date, Topic
- Grade : Id, StudentId, LessonId, Value (int?), Type (Lesson | Homework | NA)
  - Type=NA → неаттестация, красный маркер, не идёт в средний балл
- Homework : Id, LessonId, Description, FilePath, Deadline
- Announcement : Id, Title, Text, CreatedAt, AuthorId, GroupId (nullable = для всех)

## Страницы (Razor Pages)

### Student
- /Student/Index — средний балл, % посещаемости
- /Student/Schedule — список занятий группы
- /Student/Grades — таблица занятий с оценками
- /Student/Homeworks — список ДЗ со скачиванием
- /Student/Profile — ФИО, группа, смена пароля
- /Student/Announcements — лента объявлений

### Teacher
- /Teacher/Index — список занятий
- /Teacher/Lesson/{id} — посещаемость, оценки, прикрепить ДЗ, посмотреть сданные, н/б
- /Teacher/Journal/{groupId} — таблица: студенты × занятия, редактирование inline

### Admin
- /Admin/Users — список, смена роли
- /Admin/Groups — CRUD
- /Admin/Announcements — создание

### Account
- /Account/Login
- /Account/Register (присваивает роль Student)
- /Account/Logout

## Файлы
- wwwroot/uploads/{groupId}/{lessonId}/
- В БД хранить относительный путь

## Ограничения
- Простота важнее "правильности"
- Никаких JWT, внешних API, сложного JS
- Код должен компилироваться
- Комментарии короткие, по-русски
## ЖЁСТКИЕ ПРАВИЛА ПРОСТОТЫ

- Никаких паттернов Repository, UnitOfWork, CQRS, MediatR, AutoMapper.
- Никаких JWT, OAuth, 2FA, HTTPS-only, антифорджери, если это не требуется.
- Пароли хранить в открытом виде (учебный проект).
- Контроллеры и Razor Pages использовать напрямую с DbContext через DI.
- Минимум JS. Только Bootstrap 5 + встроенный JS.
- Календарь — обычная HTML-таблица, без библиотек.
- Если сомневаешься между "правильно" и "просто" — выбирай "просто".
- Комментарии — короткие, по-русски, только там, где не очевидно.