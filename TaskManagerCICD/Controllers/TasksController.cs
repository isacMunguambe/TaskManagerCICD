using Microsoft.AspNetCore.Mvc;
using TaskManagerCICD.Models;
using TaskManagerCICD.Services;

namespace TaskManagerCICD.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController(ITaskService tasks) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<TaskItem>> GetAll() => Ok(tasks.GetAll());

    [HttpGet("{id:int}")]
    public ActionResult<TaskItem> GetById(int id) =>
        tasks.GetById(id) is { } task ? Ok(task) : NotFound();

    [HttpPost]
    public ActionResult<TaskItem> Create(CreateTaskRequest request)
    {
        try
        {
            var task = tasks.Create(request);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}/complete")]
    public ActionResult<TaskItem> Complete(int id)
    {
        try
        {
            return Ok(tasks.Complete(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) => tasks.Delete(id) ? NoContent() : NotFound();
}
