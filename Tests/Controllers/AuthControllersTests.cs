using DigipetApi.Api;
using DigipetApi.Api.Controllers;
using DigipetApi.Api.Data;
using DigipetApi.Api.Dtos.Auth;
using DigipetApi.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using System.Linq;
using Xunit;
using Microsoft.Extensions.Options;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System;

namespace DigipetApi.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly ApplicationDBContext _context;
    private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<ApplicationDBContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        _context = new ApplicationDBContext(options);
        _mockJwtSettings = new Mock<IOptions<JwtSettings>>();
        _mockJwtSettings.Setup(x => x.Value).Returns(new JwtSettings
        {
            SecretKey = "this-is-a-long-enough-secret-key-for-hs256-algorithm",
            Issuer = "your-issuer",
            Audience = "your-audience",
            ExpirationInMinutes = 60,
            RefreshTokenExpirationInDays = 7
        });

        _controller = new AuthController(_context, _mockJwtSettings.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private string GenerateTestJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_mockJwtSettings.Object.Value.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _mockJwtSettings.Object.Value.Issuer,
            audience: _mockJwtSettings.Object.Value.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(-5), // Expired token
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ========================== Register Tests ==========================
    [Fact]
    public void Register_ValidInput_ReturnsOkResult()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = _controller.Register(registerDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User registered successfully", okResult.Value);

        // Verify the user was added to the database
        var user = _context.Users.SingleOrDefault(u => u.Username == registerDto.Username);
        Assert.NotNull(user);
        Assert.Equal(registerDto.Email, user.Email);
    }

    [Fact]
    public void Register_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto();
        _controller.ModelState.AddModelError("Username", "Username is required");

        // Act
        var result = _controller.Register(registerDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Register_ExistingUsername_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "password123"
        };

        _context.Users.Add(new User { Username = "existinguser" });
        _context.SaveChanges();

        // Act
        var result = _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var serializableError = Assert.IsType<SerializableError>(badRequestResult.Value);
        Assert.Contains("Username", serializableError.Keys);
        var errorMessages = Assert.IsType<string[]>(serializableError["Username"]);
        Assert.Contains("Username already exists", errorMessages);
    }

    [Fact]
    public void Register_ExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "password123"
        };

        _context.Users.Add(new User { Email = "existing@example.com" });
        _context.SaveChanges();

        // Act
        var result = _controller.Register(registerDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var serializableError = Assert.IsType<SerializableError>(badRequestResult.Value);
        Assert.Contains("Email", serializableError.Keys);
        var errorMessages = Assert.IsType<string[]>(serializableError["Email"]);
        Assert.Contains("Email already exists", errorMessages);
    }

    // ========================== Login Tests ==========================
    [Fact]
    public void Login_ValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var loginDto = new LoginDto
        {
            UsernameOrEmail = "testuser",
            Password = "password123"
        };

        // Act
        var result = _controller.Login(loginDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);

        // Use reflection to check if the properties exist
        var type = value.GetType();
        var accessTokenProperty = type.GetProperty("token");
        var refreshTokenProperty = type.GetProperty("refreshToken");

        Assert.NotNull(accessTokenProperty);
        Assert.NotNull(refreshTokenProperty);

        var accessToken = accessTokenProperty.GetValue(value);
        var refreshToken = refreshTokenProperty.GetValue(value);

        Assert.NotNull(accessToken);
        Assert.NotNull(refreshToken);
    }

    [Fact]
    public void Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            UsernameOrEmail = "testuser",
            Password = "wrongpassword"
        };

        // Act
        var result = _controller.Login(loginDto);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid username/email or password", unauthorizedResult.Value);
    }

    // ========================== RefreshToken Tests ==========================
    [Fact]
    public void RefreshToken_ValidToken_ReturnsOkResult()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "testuser",
            Email = "test@example.com",
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var validAccessToken = GenerateTestJwtToken(user);
        var refreshTokenDto = new RefreshTokenDto
        {
            AccessToken = validAccessToken,
            RefreshToken = "valid-refresh-token"
        };

        // Act
        var result = _controller.RefreshToken(refreshTokenDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);

        // Use reflection to check if the properties exist
        var type = value.GetType();
        var accessTokenProperty = type.GetProperty("accessToken");
        var refreshTokenProperty = type.GetProperty("refreshToken");

        Assert.NotNull(accessTokenProperty);
        Assert.NotNull(refreshTokenProperty);

        var accessToken = accessTokenProperty.GetValue(value);
        var refreshToken = refreshTokenProperty.GetValue(value);

        Assert.NotNull(accessToken);
        Assert.NotNull(refreshToken);
    }

    [Fact]
    public void RefreshToken_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "testuser",
            Email = "test@example.com",
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(-1) // Expired refresh token
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        var expiredAccessToken = GenerateTestJwtToken(user);
        var refreshTokenDto = new RefreshTokenDto
        {
            AccessToken = expiredAccessToken,
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var result = _controller.RefreshToken(refreshTokenDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid access token or refresh token", badRequestResult.Value);
    }

    // ========================== Logout Tests ==========================
    [Fact]
    public void Logout_AuthenticatedUser_ReturnsOkResult()
    {
        // Arrange
        var user = new User
        {
            UserId = 1,
            Username = "testuser",
            Email = "test@example.com",
            RefreshToken = "valid-refresh-token"
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        SetupUserClaim(_controller, "testuser");

        // Act
        var result = _controller.Logout();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("User logged out successfully", okResult.Value);
    }

    [Fact]
    public void Logout_UnauthenticatedUser_ReturnsBadRequest()
    {
        // Arrange
        SetupUserClaim(_controller, "nonexistentuser");

        // Act
        var result = _controller.Logout();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid user name", badRequestResult.Value);
    }

    private void SetupUserClaim(AuthController controller, string username)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
}