using TaskManagerCICD.Models;
using TaskManagerCICD.Services;

namespace Tests;

public class InMemoryTaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static InMemoryTaskService CreateService() => new(new FixedTimeProvider(Now));

    [Fact]
    public void Create_WithValidData_AssignsIdAndCreatedAt()
    {
        var service = CreateService();

        var task = service.Create(new CreateTaskRequest("Estudar CI", null, null));

        Assert.Equal(1, task.Id);
        Assert.Equal("Estudar CI", task.Title);
        Assert.False(task.IsCompleted);
        Assert.Equal(Now.UtcDateTime, task.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTitle_Throws(string title)
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.Create(new CreateTaskRequest(title, null, null)));
    }

    [Fact]
    public void Create_WithTitleOverMaxLength_Throws()
    {
        var service = CreateService();
        var title = new string('a', InMemoryTaskService.MaxTitleLength + 1);

        Assert.Throws<ArgumentException>(() => service.Create(new CreateTaskRequest(title, null, null)));
    }

    [Fact]
    public void Create_WithPastDueDate_Throws()
    {
        var service = CreateService();
        var yesterday = Now.UtcDateTime.AddDays(-1);

        Assert.Throws<ArgumentException>(() => service.Create(new CreateTaskRequest("Tarefa", null, yesterday)));
    }

    [Fact]
    public void Create_WithFutureDueDate_Succeeds()
    {
        var service = CreateService();
        var tomorrow = Now.UtcDateTime.AddDays(1);

        var task = service.Create(new CreateTaskRequest("Tarefa", null, tomorrow));

        Assert.Equal(tomorrow, task.DueDate);
    }

    [Fact]
    public void Create_WithoutPriority_DefaultsToMedium()
    {
        var service = CreateService();

        var task = service.Create(new CreateTaskRequest("Tarefa", null, null));

        Assert.Equal(Priority.Medium, task.Priority);
    }

    [Theory]
    [InlineData(Priority.Low)]
    [InlineData(Priority.Medium)]
    [InlineData(Priority.High)]
    public void Create_WithPriority_KeepsPriority(Priority priority)
    {
        var service = CreateService();

        var task = service.Create(new CreateTaskRequest("Tarefa", null, null, priority));

        Assert.Equal(priority, task.Priority);
    }

    [Fact]
    public void Create_WithInvalidPriority_Throws()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() =>
            service.Create(new CreateTaskRequest("Tarefa", null, null, (Priority)99)));
    }

    [Fact]
    public void Complete_PendingTask_MarksAsCompleted()
    {
        var service = CreateService();
        var task = service.Create(new CreateTaskRequest("Tarefa", null, null));

        var completed = service.Complete(task.Id);

        Assert.True(completed.IsCompleted);
    }

    [Fact]
    public void Complete_AlreadyCompleted_Throws()
    {
        var service = CreateService();
        var task = service.Create(new CreateTaskRequest("Tarefa", null, null));
        service.Complete(task.Id);

        Assert.Throws<InvalidOperationException>(() => service.Complete(task.Id));
    }

    [Fact]
    public void Complete_UnknownId_Throws()
    {
        var service = CreateService();

        Assert.Throws<KeyNotFoundException>(() => service.Complete(999));
    }

    [Fact]
    public void Delete_ExistingTask_RemovesIt()
    {
        var service = CreateService();
        var task = service.Create(new CreateTaskRequest("Tarefa", null, null));

        Assert.True(service.Delete(task.Id));
        Assert.Null(service.GetById(task.Id));
    }

    [Fact]
    public void Delete_UnknownId_ReturnsFalse()
    {
        Assert.False(CreateService().Delete(999));
    }
}
