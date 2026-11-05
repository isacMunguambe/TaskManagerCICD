using TaskManagerCICD.Models;

namespace TaskManagerCICD.Services;

// Regras de negócio partilhadas pelas implementações de ITaskService (memória e SQL Server)
public static class TaskRules
{
    public const int MaxTitleLength = 100;

    public static void ValidateCreate(CreateTaskRequest request, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("O título é obrigatório.", nameof(request));

        if (request.Title.Length > MaxTitleLength)
            throw new ArgumentException($"O título não pode ter mais de {MaxTitleLength} caracteres.", nameof(request));

        if (!Enum.IsDefined(request.Priority))
            throw new ArgumentException("A prioridade é inválida.", nameof(request));

        if (request.DueDate is { } due && due < now)
            throw new ArgumentException("A data limite não pode estar no passado.", nameof(request));
    }

    public static void EnsureCanComplete(TaskItem task)
    {
        if (task.IsCompleted)
            throw new InvalidOperationException($"A tarefa {task.Id} já está completa.");
    }
}
