# Электронный журнал — курсовая (упрощённая версия)

## Стек
- ASP.NET Core 9, Razor Pages
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
- Subject : Id, Name
- Lesson : Id, SubjectId, GroupId, TeacherId, DateFrom, DateTo, Topic
- Grade : Id, StudentId, LessonId, Value (int?), Type (Lesson | Homework | NA)
  - Type=NA → неаттестация, красный маркер, не идёт в средний балл
- Homework : Id, LessonId, Description, FilePath, Deadline
- StudentHomework : Id, HomeworkId, StudentId, FilePath, SubmittedAt (сданная работа; оценка — в Grade, Type=Homework)
- Announcement : Id, Title, Text, CreatedAt, AuthorId, GroupId (nullable = для всех)

## Страницы (Razor Pages)

### Student
- /Student/Index — средний балл, % посещаемости
- /Student/Schedule — список занятий группы
- /Student/Grades — таблица занятий с оценками
- /Student/Homeworks — список ДЗ со скачиванием
- /Student/Profile — ФИО, группа (только чтение)
- /Student/Announcements — лента объявлений

### Teacher
- /Teacher/Index — список занятий
- /Teacher/Lesson/{id} — посещаемость, оценки, прикрепить ДЗ, посмотреть сданные, н/б
- /Teacher/Journal/{groupId} — таблица: студенты × занятия, редактирование inline

### Admin
- /Admin/Users — список, смена роли
- /Admin/Groups — список + создание
- /Admin/Announcements — создание
- /Admin/Subjects — список + создание предметов (название)
- /Admin/Schedule — список + создание занятий (предмет + группа + учитель + дата + тема)

### Account
- /Account/Login
- /Account/Logout

## Файлы
- Файлы ДЗ хранятся в wwwroot/uploads/homeworks/ (имя — GUID + расширение).
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
## ДИЗАЙН-ГАЙД (редизайн, референс: studieau.co)

Общий стиль: минимализм, много воздуха, крупная типографика, тонкие линии,
аккуратные отступы, скруглённые углы, мягкие тени, продуманная сетка.
Серо-чёрно-белый. Эмодзи — запрещены. Font Awesome / Bootstrap Icons — запрещены
(иконки только inline SVG).

### Цветовая палитра (переменные в site.css)
- --j-bg: #f5f5f5 (фон страницы)
- --j-surface: #ffffff (карточки)
- --j-ink: #16181b (основной текст, кнопки)
- --j-muted: #6c757d (вторичный текст)
- --j-line: #e5e5e6 (границы), --j-line-strong: #d3d5d8
- Опасные действия (Удалить, ошибки, "н/б"): #dc3545
- Успех ("Сдано", "Зачтено"): #198754
- ЗАПРЕЩЁН Bootstrap-синий: #0d6efd, #86b7fe, #0077cc, #1b6ec2.
  Синим не должен светиться ни один элемент: ссылки, фокус инпутов, btn-link.

### Шрифты (CDN Google Fonts)
- Inter — интерфейс (400/500/600).
- Manrope — крупные заголовки и цифры метрик (600/700).
- Фолбэк: системный стек, если CDN недоступен.

### Типографика
- h1-h3: Manrope, font-weight 500-600, letter-spacing -0.02em, тёмные.
- Микро-лейблы (eyebrow): uppercase, 11px, letter-spacing 0.1em, --j-muted.
- Основной текст: 15px, line-height 1.55.
- Крупные цифры метрик: Manrope, 40-44px, letter-spacing -0.03em.

### Layout
- Навбар: белый фон, тонкая нижняя граница, бренд с letter-spacing,
  ссылки с плавным подчёркиванием (scaleX), активная — чёрная линия.
- Контент: max-width 1200px, центрирован, воздух вокруг.
- Карточки: белый фон, border 1px solid #e5e5e6, border-radius 10px,
  тень 0 1px 2px rgba(16,18,20,.04), при hover — лёгкий подъём (translateY(-2px))
  и мягкая тень 0 8px 24px rgba(16,18,20,.08).

### Кнопки
- btn-primary: чёрный фон, белый текст, border-radius 6px, transition .18s.
- btn-outline-secondary: прозрачный фон, тёмная рамка, при hover — чёрный фон.
- btn-danger: #dc3545 (только удаление).
- btn-link / обычные ссылки: тёмный цвет, без синего, подчёркивание при hover.
- Никаких btn-success / btn-warning в интерфейсе.

### Таблицы
- Без table-striped. Шапка: uppercase, 11px, letter-spacing .08em, --j-muted.
- Только горизонтальные линии #e5e5e6, hover строки — #f5f5f5 (transition).

### Формы
- Инпуты: border 1px solid #d3d5d8, border-radius 6px, padding 9px 12px.
- Focus: тёмная рамка + мягкая тень rgba(22,24,27,.08), НЕ синяя.
- Label: font-weight 500, 14px, --j-ink.
- Валидация: красная рамка + красный текст (стандартный Bootstrap).

### Анимации (разрешены, только CSS)
- transition/transform, @keyframes fade-in (opacity + translateY).
- Класс .journal-in + стаггер через :nth-child для сеток карточек.
- Hover-lift карточек, подчёркивание ссылок через ::after + scaleX.
- Только CSS, без JS-библиотек. Уважать prefers-reduced-motion.

### Календарь (общие классы site.css)
- .cal — сетка 7 колонок с тонкими границами, radius 10px.
- .cal-head — день недели (eyebrow) + число; сегодня — чёрная плашка.
- .cal-lesson — карточка занятия с чёрной левой полосой, hover-подъём.

### Особые маркеры
- "н/б": красный #dc3545, font-weight 500. Пропуск: серый, курсив.
- Оценка 5: жирный чёрный. 4: чёрный. 3: серый. 2: красный.

### Чего НЕ делать
- Никаких ярких цветов и цветных градиентов (ч/б градиент-заглушка героя — можно).
- Никаких эмодзи, Font Awesome, Bootstrap Icons.
- Никакого тёмного режима, никаких цветных бейджей.

### Кастомный CSS
Всё в wwwroot/css/site.css, классы с префиксом journal- или cal-.
Bootstrap-классы (row, col, card, table) — использовать, переопределять в site.css.
Порядок подключения: bootstrap → Journal.styles.css → site.css (site.css последним).
## РЕГИСТРАЦИЯ И ПОЛЬЗОВАТЕЛИ

- Публичной регистрации НЕТ.
- Пользователей создаёт только Администратор на странице /Admin/Users/Create.
- При создании админ указывает: ФИО, Email, Пароль, Роль (Student/Teacher/Admin), Группу (только для Student).
- Админ может редактировать пользователя (сменить ФИО, роль, группу) и сбрасывать пароль.
- Страница регистрации не существует. Ссылки "Регистрация" в навбаре нет.
- Страница "Забыли пароль?" не нужна.
- Смена пароля — только через администратора (/Admin/Users/Edit). Пользователь сам пароль не меняет.

## Soft delete
- Удаление пользователей — мягкое (IsDeleted = true). Данные (занятия, оценки) сохраняются. Удалённые не показываются в списках и не могут войти.

## Известные ограничения (осознанные)
- Админ может снять с себя роль Admin через Edit (не защищено).
- У пользователя только одна роль.
- Настройки cookie-политики минимальные (учебный проект).
## Проверка build после каждой итерации
