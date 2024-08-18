using Microsoft.EntityFrameworkCore;
using DigipetApi.Api.Data;
using DigipetApi.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using DigipetApi.Api.Services;
using DigipetApi.Api.Interfaces;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDBContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Digipet API", Version = "v1" });

    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure JWT authentication
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is null"))),
        ClockSkew = TimeSpan.Zero
    };
});

// Add Redis caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    options.Configuration = configuration;
    options.InstanceName = "DigipetCache_";

    // Add logging
    var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
    logger.LogInformation($"Configuring Redis with connection string: {configuration}");

    // Test the connection
    try
    {
        var redis = ConnectionMultiplexer.Connect(configuration);
        redis.GetDatabase().Ping();
        logger.LogInformation("Successfully connected to Redis");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to connect to Redis");
    }
});

// Add PetCacheService
builder.Services.AddScoped<IPetCacheService, PetCacheService>();
builder.Services.AddScoped<PetCacheService>();

// Add ScheduledTaskService
builder.Services.AddHostedService<ScheduledTaskService>();

// Add PetAttributeDecayService
builder.Services.AddHostedService<PetAttributeDecayService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDBContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add this line before app.UseAuthorization();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();