using TaskManagerCICD.Models;

namespace TaskManagerCICD.Services;

public class InMemoryTaskService(TimeProvider timeProvider) : ITaskService
{
    public const int MaxTitleLength = TaskRules.MaxTitleLength;

    private readonly Lock _lock = new();
    private readonly List<TaskItem> _tasks = [];
    private int _nextId = 1;

    public IReadOnlyList<TaskItem> GetAll()
    {
        lock (_lock) return _tasks.ToList();
    }

    public TaskItem? GetById(int id)
    {
        lock (_lock) return _tasks.FirstOrDefault(t => t.Id == id);
    }

    public TaskItem Create(CreateTaskRequest request)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        TaskRules.ValidateCreate(request, now);

        lock (_lock)
        {
            var task = new TaskItem
            {
                Id = _nextId++,
                Title = request.Title.Trim(),
                Description = request.Description,
                Priority = request.Priority,
                CreatedAt = now,
                DueDate = request.DueDate
            };
            _tasks.Add(task);
            return task;
        }
    }

    public TaskItem Complete(int id)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id)
                ?? throw new KeyNotFoundException($"Tarefa {id} não encontrada.");

            TaskRules.EnsureCanComplete(task);

            task.IsCompleted = true;
            return task;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock) return _tasks.RemoveAll(t => t.Id == id) > 0;
    }
}
