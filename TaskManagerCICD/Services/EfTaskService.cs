using Microsoft.EntityFrameworkCore;
using TaskManagerCICD.Data;
using TaskManagerCICD.Models;

namespace TaskManagerCICD.Services;

public class EfTaskService(TaskManagerDbContext db, TimeProvider timeProvider) : ITaskService
{
    public IReadOnlyList<TaskItem> GetAll() =>
        db.Tasks.AsNoTracking().OrderBy(t => t.Id).ToList();

    public TaskItem? GetById(int id) =>
        db.Tasks.AsNoTracking().FirstOrDefault(t => t.Id == id);

    public TaskItem Create(CreateTaskRequest request)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        TaskRules.ValidateCreate(request, now);

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority,
            CreatedAt = now,
            DueDate = request.DueDate
        };

        db.Tasks.Add(task);
        db.SaveChanges();
        return task;
    }

    public TaskItem Complete(int id)
    {
        var task = db.Tasks.Find(id)
            ?? throw new KeyNotFoundException($"Tarefa {id} não encontrada.");

        TaskRules.EnsureCanComplete(task);

        task.IsCompleted = true;
        db.SaveChanges();
        return task;
    }

    public bool Delete(int id) =>
        db.Tasks.Where(t => t.Id == id).ExecuteDelete() > 0;
}
