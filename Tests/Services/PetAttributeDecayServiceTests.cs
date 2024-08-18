using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DigipetApi.Api.Data;
using DigipetApi.Api.Dtos.Pet;
using DigipetApi.Api.Interfaces;
using DigipetApi.Api.Models;
using DigipetApi.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigipetApi.Tests.Services
{
    public class PetAttributeDecayServiceTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ILogger<PetAttributeDecayService>> _mockLogger;
        private readonly Mock<IPetCacheService> _mockCacheService;
        private readonly DbContextOptions<ApplicationDBContext> _dbContextOptions;

        public PetAttributeDecayServiceTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockLogger = new Mock<ILogger<PetAttributeDecayService>>();
            _mockCacheService = new Mock<IPetCacheService>();

            _dbContextOptions = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            var mockScope = new Mock<IServiceScope>();
            var mockScopeFactory = new Mock<IServiceScopeFactory>();

            mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
            mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);
            _mockServiceProvider.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(mockScopeFactory.Object);
            _mockServiceProvider.Setup(s => s.GetService(typeof(ApplicationDBContext))).Returns(() => new ApplicationDBContext(_dbContextOptions));
            _mockServiceProvider.Setup(s => s.GetService(typeof(IPetCacheService))).Returns(_mockCacheService.Object);
        }

        [Fact]
        public async Task ExecuteAsync_ProcessesPetAttributeDecay()
        {
            // Arrange
            using var context = new ApplicationDBContext(_dbContextOptions);
            var adoptedPets = new List<Pet>
            {
                new Pet { PetId = 1, UserId = 1, Health = 100, Mood = 100, Happiness = 100 },
                new Pet { PetId = 2, UserId = 2, Health = 50, Mood = 50, Happiness = 50 }
            };
            context.Pets.AddRange(adoptedPets);
            await context.SaveChangesAsync();

            _mockServiceProvider.Setup(s => s.GetService(typeof(ApplicationDBContext))).Returns(context);

            var service = new PetAttributeDecayService(_mockServiceProvider.Object, _mockLogger.Object, TimeSpan.FromMilliseconds(100));

            // Act
            await service.ProcessPetAttributeDecay(); // Directly call the method

            // Assert
            var updatedPets = await context.Pets.ToListAsync();
            foreach (var pet in updatedPets)
            {
                Assert.Equal(pet.PetId == 1 ? 99 : 49, pet.Health);
                Assert.Equal(pet.PetId == 1 ? 98 : 48, pet.Mood);
                Assert.Equal(pet.PetId == 1 ? 98 : 48, pet.Happiness);
                Assert.True(pet.UpdatedAt <= DateTime.UtcNow);

                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Decreased attributes for adopted pet {pet.PetId}")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);

                _mockCacheService.Verify(c => c.SetPetAsync(It.Is<PetDto>(dto => dto.PetId == pet.PetId)), Times.Once);
            }
        }
    }
}