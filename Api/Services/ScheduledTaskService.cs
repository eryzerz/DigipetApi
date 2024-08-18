using DigipetApi.Api.Data;
using DigipetApi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigipetApi.Api.Services;

public class ScheduledTaskService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScheduledTaskService> _logger;

    public ScheduledTaskService(IServiceProvider serviceProvider, ILogger<ScheduledTaskService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledTaskService is starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Processing scheduled tasks...");
            await ProcessScheduledTasks();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessScheduledTasks()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        var now = DateTime.UtcNow;
        _logger.LogInformation($"Current time: {now}");

        var tasks = await dbContext.ScheduledTasks
            .Where(t => !t.IsCompleted && t.ScheduledTime <= now && t.DaysOfWeek.Contains(now.DayOfWeek.ToString()))
            .ToListAsync();

        _logger.LogInformation($"Found {tasks.Count} tasks to process.");

        foreach (var task in tasks)
        {
            _logger.LogInformation($"Processing task {task.TaskId} for pet {task.PetId}");
            await ExecuteTask(task, dbContext);
        }
    }

    private new async Task ExecuteTask(ScheduledTask task, ApplicationDBContext dbContext)
    {
        var pet = await dbContext.Pets.FindAsync(task.PetId);
        if (pet != null)
        {
            switch (task.TaskType)
            {
                case "Feed":
                    pet.Health += 10;
                    pet.Happiness += 5;
                    pet.Health = Math.Clamp(pet.Health, 0, 100);
                    pet.Happiness = Math.Clamp(pet.Happiness, 0, 100);
                    pet.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation($"Automatic feeding executed for pet {pet.PetId}");
                    break;
                    // Add other task types here if needed
            }

            task.IsCompleted = true;
            task.ScheduledTime = task.ScheduledTime.AddDays(1);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"Task {task.TaskId} completed and rescheduled for tomorrow.");
        }
        else
        {
            _logger.LogWarning($"Pet {task.PetId} not found for task {task.TaskId}");
        }
    }
}