using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DigipetApi.Api.Dtos.Pet;
using DigipetApi.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigipetApi.Tests.Services
{
    public class PetCacheServiceTests
    {
        private readonly Mock<IDistributedCache> _mockCache;
        private readonly Mock<ILogger<PetCacheService>> _mockLogger;
        private readonly PetCacheService _petCacheService;

        public PetCacheServiceTests()
        {
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<PetCacheService>>();
            _petCacheService = new PetCacheService(_mockCache.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetPetAsync_CacheHit_ReturnsPetDto()
        {
            // Arrange
            var petId = 1;
            var petDto = new PetDto { PetId = petId, Name = "TestPet" };
            var petJson = JsonSerializer.Serialize(petDto);
            var petBytes = Encoding.UTF8.GetBytes(petJson);

            _mockCache.Setup(c => c.GetAsync($"pet_{petId}", default))
                .ReturnsAsync(petBytes);

            // Act
            var result = await _petCacheService.GetPetAsync(petId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(petId, result.PetId);
            Assert.Equal("TestPet", result.Name);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Cache hit for pet_{petId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetPetAsync_CacheMiss_ReturnsNull()
        {
            // Arrange
            var petId = 1;
            _mockCache.Setup(c => c.GetAsync($"pet_{petId}", default))
                .ReturnsAsync((byte[]?)null);

            // Act
            var result = await _petCacheService.GetPetAsync(petId);

            // Assert
            Assert.Null(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains($"Cache miss for pet_{petId}")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task SetPetAsync_SetsCache()
        {
            // Arrange
            var petDto = new PetDto { PetId = 1, Name = "TestPet" };

            // Act
            await _petCacheService.SetPetAsync(petDto);

            // Assert
            _mockCache.Verify(c => c.SetAsync(
                $"pet_{petDto.PetId}",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                default
            ), Times.Once);
        }

        [Fact]
        public async Task RemovePetAsync_RemovesFromCache()
        {
            // Arrange
            var petId = 1;

            // Act
            await _petCacheService.RemovePetAsync(petId);

            // Assert
            _mockCache.Verify(c => c.RemoveAsync($"pet_{petId}", default), Times.Once);
        }
    }
}
