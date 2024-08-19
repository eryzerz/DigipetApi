using DigipetApi.Api.Controllers;
using DigipetApi.Api.Data;
using DigipetApi.Api.Services;
using DigipetApi.Api.Dtos.Pet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DigipetApi.Api.Interfaces;
using DigipetApi.Api.Models;
using DigipetApi.Api.Dtos.ScheduledTask;
using System.Security.Claims;
using DigipetApi.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace DigipetApi.Tests.Controllers;
public class PetControllerTests
{
    private readonly ApplicationDBContext _context;
    private readonly Mock<IPetCacheService> _mockCacheService;
    private readonly Mock<ITezosService> _mockTezosService;
    private readonly Mock<ILogger<PetController>> _mockLogger;

    public PetControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
          .UseInMemoryDatabase(databaseName: "TestDatabase")
          .Options;

        _context = new ApplicationDBContext(options);
        _mockCacheService = new Mock<IPetCacheService>();
        _mockTezosService = new Mock<ITezosService>();
        _mockLogger = new Mock<ILogger<PetController>>();
    }

    private class TestPetController : PetController
    {
        private readonly DbSet<Pet> _pets;

        public TestPetController(ApplicationDBContext context, IPetCacheService cacheService, ITezosService tezosService, ILogger<PetController> logger, DbSet<Pet> pets)
          : base(context, cacheService, tezosService, logger)
        {
            _pets = pets;
        }

        protected override DbSet<Pet> Pets => _pets;
    }


    // ========================== GetById Tests ==========================
    [Fact]
    public async Task GetById_PetExistInCache_ReturnsOkResult()
    {
        // Arrange
        var petId = 1;
        var cachedPet = new PetDto { PetId = petId, Name = "TestPet" };
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync(cachedPet);

        var controller = new PetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object);

        // Act
        var result = await controller.GetById(petId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPet = Assert.IsType<PetDto>(okResult.Value);
        Assert.Equal(petId, returnedPet.PetId);
        Assert.Equal("TestPet", returnedPet.Name);
        _mockCacheService.Verify(s => s.GetPetAsync(petId), Times.Once);
        _mockCacheService.Verify(s => s.SetPetAsync(It.IsAny<PetDto>()), Times.Never);
    }

    [Fact]
    public async Task GetById_PetNotInCacheButInDatabase_ReturnsOkResult()
    {
        // Arrange
        var petId = 1;
        var dbPet = new Pet { PetId = petId, Name = "TestPet" };
        var petDto = new PetDto { PetId = petId, Name = "TestPet" };

        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync((PetDto?)null);
        _mockCacheService.Setup(s => s.SetPetAsync(It.IsAny<PetDto>())).Returns(Task.CompletedTask);

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(dbPet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = await controller.GetById(petId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPet = Assert.IsType<PetDto>(okResult.Value);
        Assert.Equal(petId, returnedPet.PetId);
        Assert.Equal("TestPet", returnedPet.Name);

        _mockCacheService.Verify(s => s.GetPetAsync(petId), Times.Once);
        _mockCacheService.Verify(s => s.SetPetAsync(It.Is<PetDto>(dto =>
            dto.PetId == petId && dto.Name == "TestPet")), Times.Once);
    }

    [Fact]
    public async Task GetById_PetNotFound_ReturnsNotFound()
    {
        // Arrange
        var petId = 999; // An ID that doesn't exist
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync((PetDto?)null);

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync((Pet?)null);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = await controller.GetById(petId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _mockCacheService.Verify(s => s.GetPetAsync(petId), Times.Once);
        _mockCacheService.Verify(s => s.SetPetAsync(It.IsAny<PetDto>()), Times.Never);
        mockSet.Verify(m => m.FindAsync(petId), Times.Once);
    }


    // ========================== GetUserPets Tests ==========================
    [Fact]
    public void GetUserPets_ReturnsOkResultWithUserPets()
    {
        // Arrange
        var currentUserId = 1;
        var userPets = new List<Pet>
        {
            new Pet { PetId = 1, Name = "Pet1", UserId = currentUserId },
            new Pet { PetId = 2, Name = "Pet2", UserId = currentUserId }
        };
        var petDtos = userPets.Select(p => new PetDto { PetId = p.PetId, Name = p.Name, UserId = p.UserId }).ToList();

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(userPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(userPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(userPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(userPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Mock the User claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = controller.GetUserPets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Equal(2, returnedPets.Count());
        Assert.All(returnedPets, pet => Assert.Equal(currentUserId, pet.UserId));
    }

    [Fact]
    public void GetUserPets_ReturnsEmptyList_WhenUserHasNoPets()
    {
        // Arrange
        var currentUserId = 1;
        var userPets = new List<Pet>();

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(userPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(userPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(userPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(userPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Mock the User claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = controller.GetUserPets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Empty(returnedPets);
    }

    [Fact]
    public void GetUserPets_ReturnsOnlyUserOwnedPets_WhenOtherUsersHavePets()
    {
        // Arrange
        var currentUserId = 1;
        var otherUserId = 2;
        var allPets = new List<Pet>
        {
            new Pet { PetId = 1, Name = "Pet1", UserId = currentUserId },
            new Pet { PetId = 2, Name = "Pet2", UserId = otherUserId },
            new Pet { PetId = 3, Name = "Pet3", UserId = currentUserId }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(allPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(allPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(allPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(allPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Mock the User claim
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = controller.GetUserPets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Equal(2, returnedPets.Count());
        Assert.All(returnedPets, pet => Assert.Equal(currentUserId, pet.UserId));
        Assert.DoesNotContain(returnedPets, pet => pet.UserId == otherUserId);
    }

    [Fact]
    public void GetUserPets_HandlesInvalidUserIdClaim()
    {
        // Arrange
        var invalidUserId = "invalid";
        var allPets = new List<Pet>();

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(allPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(allPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(allPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(allPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Mock the User claim with invalid ID
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, invalidUserId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() => controller.GetUserPets());
        Assert.Equal("The input string 'invalid' was not in a correct format.", exception.Message);
    }

    [Fact]
    public void GetUserPets_HandlesNullUserIdClaim()
    {
        // Arrange
        var allPets = new List<Pet>();

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(allPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(allPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(allPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(allPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Mock the User claim without NameIdentifier
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = controller.GetUserPets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Empty(returnedPets);
    }


    // ========================== GetAvailablePets Tests ==========================
    [Fact]
    public void GetAvailablePets_ReturnsOnlyAvailablePets_WhenBothAdoptedAndAvailablePetsExist()
    {
        // Arrange
        var allPets = new List<Pet>
        {
            new Pet { PetId = 1, Name = "Pet1", UserId = 1 },
            new Pet { PetId = 2, Name = "Pet2", UserId = null },
            new Pet { PetId = 3, Name = "Pet3", UserId = null },
            new Pet { PetId = 4, Name = "Pet4", UserId = 2 }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(allPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(allPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(allPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(allPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = controller.GetAvailablePets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Equal(2, returnedPets.Count());
        Assert.All(returnedPets, pet => Assert.Null(pet.UserId));
        Assert.DoesNotContain(returnedPets, pet => pet.UserId != null);
        Assert.Contains(returnedPets, pet => pet.PetId == 2);
        Assert.Contains(returnedPets, pet => pet.PetId == 3);
    }

    [Fact]
    public void GetAvailablePets_ReturnsEmptyList_WhenNoAvailablePets()
    {
        // Arrange
        var allPets = new List<Pet>
        {
            new Pet { PetId = 1, Name = "Pet1", UserId = 1 },
            new Pet { PetId = 2, Name = "Pet2", UserId = 2 },
            new Pet { PetId = 3, Name = "Pet3", UserId = 3 }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(allPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(allPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(allPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(allPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = controller.GetAvailablePets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Empty(returnedPets);
    }

    [Fact]
    public void GetAvailablePets_ReturnsAllPets_WhenAllPetsAreAvailable()
    {
        // Arrange
        var allPets = new List<Pet>
        {
            new Pet { PetId = 1, Name = "Pet1", UserId = null },
            new Pet { PetId = 2, Name = "Pet2", UserId = null },
            new Pet { PetId = 3, Name = "Pet3", UserId = null }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(allPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(allPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(allPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(allPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = controller.GetAvailablePets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Equal(3, returnedPets.Count());
        Assert.All(returnedPets, pet => Assert.Null(pet.UserId));
        Assert.Contains(returnedPets, pet => pet.PetId == 1);
        Assert.Contains(returnedPets, pet => pet.PetId == 2);
        Assert.Contains(returnedPets, pet => pet.PetId == 3);
    }

    [Fact]
    public void GetAvailablePets_ReturnsCorrectPetDtos()
    {
        // Arrange
        var allPets = new List<Pet>
        {
            new Pet { PetId = 1, Name = "Pet1", UserId = null, Species = "Dog", Type = "Labrador" },
            new Pet { PetId = 2, Name = "Pet2", UserId = 1 },
            new Pet { PetId = 3, Name = "Pet3", UserId = null, Species = "Cat", Type = "Siamese" }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Provider).Returns(allPets.AsQueryable().Provider);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.Expression).Returns(allPets.AsQueryable().Expression);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.ElementType).Returns(allPets.AsQueryable().ElementType);
        mockSet.As<IQueryable<Pet>>().Setup(m => m.GetEnumerator()).Returns(allPets.GetEnumerator());

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = controller.GetAvailablePets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPets = Assert.IsAssignableFrom<IEnumerable<PetDto>>(okResult.Value);
        Assert.Equal(2, returnedPets.Count());

        var pet1 = returnedPets.FirstOrDefault(p => p.PetId == 1);
        Assert.NotNull(pet1);
        Assert.Equal("Pet1", pet1.Name);
        Assert.Equal("Dog", pet1.Species);
        Assert.Equal("Labrador", pet1.Type);

        var pet3 = returnedPets.FirstOrDefault(p => p.PetId == 3);
        Assert.NotNull(pet3);
        Assert.Equal("Pet3", pet3.Name);
        Assert.Equal("Cat", pet3.Species);
        Assert.Equal("Siamese", pet3.Type);
    }

    // ========================== AdoptPet Tests ==========================
    [Fact]
    public async Task AdoptPet_PetExists_NotAdopted_ReturnsOkResult()
    {
        // Arrange
        var petId = 1;
        var currentUserId = 2;
        var initPet = new Pet { PetId = petId, UserId = null };
        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(initPet);

        _mockTezosService.Setup(s => s.MintPet(It.IsAny<Pet>())).ReturnsAsync("fakeTransactionHash");

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, currentUserId);

        // Act
        var result = await controller.AdoptPet(petId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var adoptedPetResult = okResult.Value;

        // Use reflection to access properties of the anonymous type
        var petProperty = adoptedPetResult.GetType().GetProperty("pet");
        Assert.NotNull(petProperty);
        var petValue = petProperty.GetValue(adoptedPetResult);
        Assert.IsType<PetDto>(petValue);
        var pet = (PetDto)petValue;

        var transactionHashProperty = adoptedPetResult.GetType().GetProperty("transactionHash");
        Assert.NotNull(transactionHashProperty);
        var transactionHashValue = transactionHashProperty.GetValue(adoptedPetResult);
        Assert.IsType<string>(transactionHashValue);
        var transactionHash = (string)transactionHashValue;

        // Assert pet properties
        Assert.Equal(petId, pet.PetId);
        Assert.Equal(currentUserId, pet.UserId);
        Assert.Equal(100, pet.Health);
        Assert.Equal(100, pet.Mood);
        Assert.Equal(100, pet.Happiness);

        // Assert transactionHash
        Assert.Equal("fakeTransactionHash", transactionHash);

        _mockCacheService.Verify(s => s.SetPetAsync(It.IsAny<PetDto>()), Times.Once);
        _mockTezosService.Verify(s => s.MintPet(It.IsAny<Pet>()), Times.Once);
    }

    [Fact]
    public async Task AdoptPet_PetNotFound_ReturnsNotFound()
    {
        // Arrange
        var petId = 999;
        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync((Pet?)null);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = await controller.AdoptPet(petId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task AdoptPet_PetAlreadyAdopted_ReturnsBadRequest()
    {
        // Arrange
        var petId = 1;
        var pet = new Pet { PetId = petId, UserId = 3 };
        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(pet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = await controller.AdoptPet(petId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("This pet is already adopted.", badRequestResult.Value);
    }

    [Fact]
    public async Task AdoptPet_TezosServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var petId = 1;
        var currentUserId = 2;
        var pet = new Pet { PetId = petId, UserId = null };
        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(pet);

        _mockTezosService.Setup(s => s.MintPet(It.IsAny<Pet>())).ThrowsAsync(new Exception("Tezos service error"));

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, currentUserId);

        // Act
        var result = await controller.AdoptPet(petId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("An error occurred while adopting the pet. Please try again later.", statusCodeResult.Value);
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Error adopting pet {petId}")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            ),
            Times.Once
        );
    }

    private void SetupUserClaim(ControllerBase controller, int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    // ========================== Interact Tests ==========================
    [Fact]
    public async Task Interact_PetExistsInCache_UpdatesPetAttributes()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var cachedPet = new PetDto { PetId = petId, UserId = userId, Health = 50, Mood = 50, Happiness = 50 };
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync(cachedPet);
        _mockCacheService.Setup(s => s.SetPetAsync(It.IsAny<PetDto>())).Returns(Task.CompletedTask);

        var mockSet = new Mock<DbSet<Pet>>();
        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        var interactDto = new InteractPetDto { Type = InteractionType.feed };

        // Act
        var result = await controller.Interact(petId, interactDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedPet = Assert.IsType<PetDto>(okResult.Value);
        Assert.Equal(60, updatedPet.Health);
        Assert.Equal(55, updatedPet.Happiness);
        _mockCacheService.Verify(s => s.SetPetAsync(It.IsAny<PetDto>()), Times.Once);
    }

    [Fact]
    public async Task Interact_PetNotInCacheButInDatabase_UpdatesPetAttributes()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var dbPet = new Pet { PetId = petId, UserId = userId, Health = 50, Mood = 50, Happiness = 50 };
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync((PetDto?)null);
        _mockCacheService.Setup(s => s.SetPetAsync(It.IsAny<PetDto>())).Returns(Task.CompletedTask);

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(dbPet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        var interactDto = new InteractPetDto { Type = InteractionType.play };

        // Act
        var result = await controller.Interact(petId, interactDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedPet = Assert.IsType<PetDto>(okResult.Value);
        Assert.Equal(45, updatedPet.Health);
        Assert.Equal(65, updatedPet.Mood);
        Assert.Equal(60, updatedPet.Happiness);
        _mockCacheService.Verify(s => s.SetPetAsync(It.IsAny<PetDto>()), Times.Once);
    }

    [Fact]
    public async Task Interact_PetNotFound_ReturnsNotFound()
    {
        // Arrange
        var petId = 1;
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync((PetDto?)null);

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync((Pet?)null);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        var interactDto = new InteractPetDto { Type = InteractionType.feed };

        // Act
        var result = await controller.Interact(petId, interactDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Interact_UserDoesNotOwnPet_ReturnsUnauthorized()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var differentUserId = 2;
        var mockSet = new Mock<DbSet<Pet>>();
        var cachedPet = new PetDto { PetId = petId, UserId = userId };
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync(cachedPet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, differentUserId);

        var interactDto = new InteractPetDto { Type = InteractionType.feed };

        // Act
        var result = await controller.Interact(petId, interactDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("You can only interact with your own pets.", unauthorizedResult.Value);
    }

    [Fact]
    public async Task Interact_InvalidInteractionType_ReturnsBadRequest()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var mockSet = new Mock<DbSet<Pet>>();
        var cachedPet = new PetDto { PetId = petId, UserId = userId };
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync(cachedPet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        var interactDto = new InteractPetDto { Type = (InteractionType)999 };

        // Act
        var result = await controller.Interact(petId, interactDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid interaction type", badRequestResult.Value);
    }

    [Theory]
    [InlineData(InteractionType.train, 55, 40, 65)]
    [InlineData(InteractionType.groom, 65, 45, 55)]
    [InlineData(InteractionType.adventure, 40, 70, 65)]
    public async Task Interact_DifferentInteractionTypes_UpdatesAttributesCorrectly(InteractionType interactionType, int expectedHealth, int expectedMood, int expectedHappiness)
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var mockSet = new Mock<DbSet<Pet>>();
        var cachedPet = new PetDto { PetId = petId, UserId = userId, Health = 50, Mood = 50, Happiness = 50 };
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync(cachedPet);
        _mockCacheService.Setup(s => s.SetPetAsync(It.IsAny<PetDto>())).Returns(Task.CompletedTask);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        var interactDto = new InteractPetDto { Type = interactionType };

        // Act
        var result = await controller.Interact(petId, interactDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedPet = Assert.IsType<PetDto>(okResult.Value);
        Assert.Equal(expectedHealth, updatedPet.Health);
        Assert.Equal(expectedMood, updatedPet.Mood);
        Assert.Equal(expectedHappiness, updatedPet.Happiness);
    }

    [Fact]
    public async Task Interact_AttributesExceedLimits_ClampsToBoundaries()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var mockSet = new Mock<DbSet<Pet>>();
        var cachedPet = new PetDto { PetId = petId, UserId = userId, Health = 95, Mood = 95, Happiness = 95 };
        _mockCacheService.Setup(s => s.GetPetAsync(petId)).ReturnsAsync(cachedPet);
        _mockCacheService.Setup(s => s.SetPetAsync(It.IsAny<PetDto>())).Returns(Task.CompletedTask);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        var interactDto = new InteractPetDto { Type = InteractionType.feed };

        // Act
        var result = await controller.Interact(petId, interactDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedPet = Assert.IsType<PetDto>(okResult.Value);
        Assert.Equal(100, updatedPet.Health);
        Assert.Equal(95, updatedPet.Mood);
        Assert.Equal(100, updatedPet.Happiness);
    }

    // ========================== ScheduleFeeding Tests ==========================
    [Fact]
    public async Task ScheduleFeeding_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var pet = new Pet { PetId = petId, UserId = userId };
        var scheduleFeedingDto = new ScheduleFeedingDto
        {
            PetId = petId,
            FeedingTime = "12:00:00",
            DaysOfWeek = new[] { "Monday", "Wednesday", "Friday" }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(pet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        // Act
        var result = await controller.ScheduleFeeding(scheduleFeedingDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Feeding schedule created successfully", okResult.Value);
    }

    [Fact]
    public async Task ScheduleFeeding_PetNotFound_ReturnsNotFoundResult()
    {
        // Arrange
        var petId = 1;
        var scheduleFeedingDto = new ScheduleFeedingDto
        {
            PetId = petId,
            FeedingTime = "12:00:00",
            DaysOfWeek = new[] { "Monday", "Wednesday", "Friday" }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync((Pet?)null);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);

        // Act
        var result = await controller.ScheduleFeeding(scheduleFeedingDto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ScheduleFeeding_UserDoesNotOwnPet_ReturnsUnauthorizedResult()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var pet = new Pet { PetId = petId, UserId = 2 }; // Different user ID
        var scheduleFeedingDto = new ScheduleFeedingDto
        {
            PetId = petId,
            FeedingTime = "12:00:00",
            DaysOfWeek = new[] { "Monday", "Wednesday", "Friday" }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(pet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        // Act
        var result = await controller.ScheduleFeeding(scheduleFeedingDto);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ScheduleFeeding_InvalidTimeFormat_ReturnsBadRequestResult()
    {
        // Arrange
        var petId = 1;
        var userId = 1;
        var pet = new Pet { PetId = petId, UserId = userId };
        var scheduleFeedingDto = new ScheduleFeedingDto
        {
            PetId = petId,
            FeedingTime = "InvalidTime",
            DaysOfWeek = new[] { "Monday", "Wednesday", "Friday" }
        };

        var mockSet = new Mock<DbSet<Pet>>();
        mockSet.Setup(m => m.FindAsync(petId)).ReturnsAsync(pet);

        var controller = new TestPetController(_context, _mockCacheService.Object, _mockTezosService.Object, _mockLogger.Object, mockSet.Object);
        SetupUserClaim(controller, userId);

        // Act
        var result = await controller.ScheduleFeeding(scheduleFeedingDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    private void SetupUserClaim(TestPetController controller, int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}