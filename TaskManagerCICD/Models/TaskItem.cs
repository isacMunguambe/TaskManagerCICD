namespace TaskManagerCICD.Models;

public enum Priority
{
    Low,
    Medium,
    High
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
}

public record CreateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDate,
    Priority Priority = Priority.Medium);
