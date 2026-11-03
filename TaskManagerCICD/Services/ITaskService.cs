using TaskManagerCICD.Models;

namespace TaskManagerCICD.Services;

public interface ITaskService
{
    IReadOnlyList<TaskItem> GetAll();
    TaskItem? GetById(int id);

    /// <exception cref="ArgumentException">Título vazio ou com mais de 100 caracteres, ou data limite no passado.</exception>
    TaskItem Create(CreateTaskRequest request);

    /// <exception cref="KeyNotFoundException">A tarefa não existe.</exception>
    /// <exception cref="InvalidOperationException">A tarefa já estava completa.</exception>
    TaskItem Complete(int id);

    bool Delete(int id);
}
