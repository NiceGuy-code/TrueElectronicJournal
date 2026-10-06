using Journal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Journal.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Homework> Homeworks => Set<Homework>();
    public DbSet<StudentHomework> StudentHomeworks => Set<StudentHomework>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Не удалять пользователя каскадом вместе с занятием/объявлением
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Teacher)
            .WithMany()
            .HasForeignKey(l => l.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Сданная работа: студента не удалять каскадом, ДЗ — удалять вместе со сдачами
        modelBuilder.Entity<StudentHomework>()
            .HasOne(s => s.Student).WithMany(u => u.SubmittedHomeworks)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StudentHomework>()
            .HasOne(s => s.Homework).WithMany(h => h.StudentHomeworks)
            .HasForeignKey(s => s.HomeworkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
