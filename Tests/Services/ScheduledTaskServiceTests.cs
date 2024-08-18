using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DigipetApi.Api.Data;
using DigipetApi.Api.Models;
using DigipetApi.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigipetApi.Tests.Services
{
  public class ScheduledTaskServiceTests : IDisposable
  {
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<ScheduledTaskService>> _mockLogger;
    private readonly ApplicationDBContext _context;

    public ScheduledTaskServiceTests()
    {
      _mockServiceProvider = new Mock<IServiceProvider>();
      _mockLogger = new Mock<ILogger<ScheduledTaskService>>();

      var options = new DbContextOptionsBuilder<ApplicationDBContext>()
          .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
          .Options;

      _context = new ApplicationDBContext(options);

      var mockServiceScope = new Mock<IServiceScope>();
      mockServiceScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

      var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
      mockServiceScopeFactory
          .Setup(x => x.CreateScope())
          .Returns(mockServiceScope.Object);

      _mockServiceProvider
          .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
          .Returns(mockServiceScopeFactory.Object);

      _mockServiceProvider
          .Setup(x => x.GetService(typeof(ApplicationDBContext)))
          .Returns(_context);
    }

    public void Dispose()
    {
      _context.Database.EnsureDeleted();
      _context.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesScheduledTasks()
    {
      // Arrange
      var service = new ScheduledTaskService(_mockServiceProvider.Object, _mockLogger.Object);
      var cts = new CancellationTokenSource();

      var mockTasks = new List<ScheduledTask>
        {
            new ScheduledTask
            {
                TaskId = 1,
                PetId = 1,
                TaskType = "Feed",
                ScheduledTime = DateTime.UtcNow.AddMinutes(-5),
                DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
                IsCompleted = false
            }
        };

      _context.ScheduledTasks.AddRange(mockTasks);
      _context.SaveChanges();

      var mockPet = new Pet { PetId = 1, Health = 50, Happiness = 50 };
      _context.Pets.Add(mockPet);
      _context.SaveChanges();

      // Act
      var executeTask = service.StartAsync(cts.Token);
      await Task.Delay(2000); // Wait for 2 seconds to allow the service to run
      cts.Cancel();
      await executeTask;

      // Assert
      _mockLogger.Verify(
          x => x.Log(
              LogLevel.Information,
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("ScheduledTaskService is starting.")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.Once);

      // Add more assertions as needed

      var updatedPet = await _context.Pets.FindAsync(1);
      Assert.NotNull(updatedPet);
      Assert.Equal(60, updatedPet.Health);
      Assert.Equal(55, updatedPet.Happiness);
    }

    [Fact]
    public async Task ExecuteAsync_PetNotFound_LogsWarning()
    {
      // Arrange
      var service = new ScheduledTaskService(_mockServiceProvider.Object, _mockLogger.Object);
      var cts = new CancellationTokenSource();

      var task = new ScheduledTask
      {
        TaskId = 1,
        PetId = 999, // Non-existent pet ID
        TaskType = "Feed",
        ScheduledTime = DateTime.UtcNow,
        DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
        IsCompleted = false
      };

      _context.ScheduledTasks.Add(task);
      _context.SaveChanges();

      // Act
      var executeTask = service.StartAsync(cts.Token);
      await Task.Delay(2000); // Wait for 2 seconds to allow the service to run
      cts.Cancel();
      await executeTask;

      // Assert
      _mockLogger.Verify(
          x => x.Log(
              LogLevel.Warning,
              It.IsAny<EventId>(),
              It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Pet 999 not found for task 1")),
              It.IsAny<Exception>(),
              It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
          Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTaskType_DoesNotModifyPet()
    {
      // Arrange
      var service = new ScheduledTaskService(_mockServiceProvider.Object, _mockLogger.Object);
      var cts = new CancellationTokenSource();

      var task = new ScheduledTask
      {
        TaskId = 1,
        PetId = 1,
        TaskType = "UnknownType",
        ScheduledTime = DateTime.UtcNow.AddHours(2),
        DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
        IsCompleted = false
      };

      _context.ScheduledTasks.Add(task);
      _context.SaveChanges();

      var mockPet = new Pet { PetId = 1, Health = 50, Happiness = 50 };
      _context.Pets.Add(mockPet);
      _context.SaveChanges();

      // Act
      var executeTask = service.StartAsync(cts.Token);
      await Task.Delay(2000); // Wait for 2 seconds to allow the service to run
      cts.Cancel();
      await executeTask;

      // Assert
      var updatedPet = await _context.Pets.FindAsync(1);
      Assert.NotNull(updatedPet);
      Assert.Equal(50, updatedPet.Health);
      Assert.Equal(50, updatedPet.Happiness);

      var updatedTask = await _context.ScheduledTasks.FindAsync(1);
      Assert.NotNull(updatedTask);
      Assert.False(updatedTask.IsCompleted);
    }
  }
}