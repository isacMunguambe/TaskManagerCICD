using Microsoft.EntityFrameworkCore;
using TaskManagerCICD.Models;
using TaskManagerCICD.Services;

namespace TaskManagerCICD.Data;

public class TaskManagerDbContext(DbContextOptions<TaskManagerDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(task =>
        {
            task.Property(t => t.Title).IsRequired().HasMaxLength(TaskRules.MaxTitleLength);

            // Guarda "Low"/"Medium"/"High" em texto, mais legível na base de dados do que 0/1/2
            task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
        });
    }
}
