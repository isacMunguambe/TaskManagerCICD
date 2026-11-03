using TaskManagerCICD.Models;

namespace TaskManagerCICD.Services;

public class InMemoryTaskService(TimeProvider timeProvider) : ITaskService
{
    public const int MaxTitleLength = 100;

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
        //if (string.IsNullOrWhiteSpace(request.Title))
        //    throw new ArgumentException("O título é obrigatório.", nameof(request));

        if (request.Title.Length > MaxTitleLength)
            throw new ArgumentException($"O título não pode ter mais de {MaxTitleLength} caracteres.", nameof(request));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (request.DueDate is { } due && due < now)
            throw new ArgumentException("A data limite não pode estar no passado.", nameof(request));

        lock (_lock)
        {
            var task = new TaskItem
            {
                Id = _nextId++,
                Title = request.Title.Trim(),
                Description = request.Description,
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

            if (task.IsCompleted)
                throw new InvalidOperationException($"A tarefa {id} já está completa.");

            task.IsCompleted = true;
            return task;
        }
    }

    public bool Delete(int id)
    {
        lock (_lock) return _tasks.RemoveAll(t => t.Id == id) > 0;
    }
}
