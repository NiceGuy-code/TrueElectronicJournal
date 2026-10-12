namespace Journal.Pages.Shared;

// Данные недельного календаря для частичного представления _WeekGrid
public class WeekCalVm
{
    public DateTime WeekStart { get; set; }
    public DateTime Today { get; set; }
    public List<DateTime> WeekDays { get; set; } = new();
    public List<WeekCalLesson> Lessons { get; set; } = new();
}

public class WeekCalLesson
{
    public int Id { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string SubjectName { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string TeacherName { get; set; } = "";
    public string Topic { get; set; } = "";

    // Готовый адрес перехода по клику (у каждой роли свой)
    public string Href { get; set; } = "";
}
