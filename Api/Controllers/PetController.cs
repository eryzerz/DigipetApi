using DigipetApi.Api.Data;
using DigipetApi.Api.Mappers;
using DigipetApi.Api.Dtos.Pet;
using DigipetApi.Api.Dtos.ScheduledTask;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DigipetApi.Api.Services;
using DigipetApi.Api.Models;
using DigipetApi.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace DigipetApi.Api.Controllers;

[ApiController]
[Route("api/pet")]
[Authorize]
public class PetController : ControllerBase
{
    private readonly ApplicationDBContext _context;
    private readonly IPetCacheService _cacheService;
    private readonly ITezosService _tezosService;
    private readonly ILogger<PetController> _logger;

    public PetController(ApplicationDBContext context, IPetCacheService cacheService, ITezosService tezosService, ILogger<PetController> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _tezosService = tezosService;
        _logger = logger;
    }

    // Change this to a protected virtual property
    protected virtual DbSet<Pet> Pets => _context.Pets;

    [HttpGet]
    public IActionResult GetAll()
    {
        var pets = Pets.ToList()
          .Select(s => s.ToPetDto());

        return Ok(pets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var cachedPet = await _cacheService.GetPetAsync(id);
        if (cachedPet != null)
        {
            return Ok(cachedPet);
        }

        var pet = await Pets.FindAsync(id);

        if (pet == null)
        {
            return NotFound();
        }

        var petDto = pet.ToPetDto();
        await _cacheService.SetPetAsync(petDto);

        return Ok(petDto);
    }

    [HttpGet("user")]
    public IActionResult GetUserPets()
    {
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var pets = Pets
            .Where(p => p.UserId == currentUserId)
            .ToList()
            .Select(s => s.ToPetDto());

        return Ok(pets);
    }

    [HttpGet("available")]
    public IActionResult GetAvailablePets()
    {
        var availablePets = Pets
            .Where(p => p.UserId == null)
            .ToList()
            .Select(p => p.ToPetDto());

        return Ok(availablePets);
    }

    [HttpPatch("{id}/adopt")]
    public async Task<IActionResult> AdoptPet(int id)
    {
        var pet = await Pets.FindAsync(id);

        if (pet == null)
        {
            return NotFound();
        }

        if (pet.UserId != null)
        {
            return BadRequest("This pet is already adopted.");
        }

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        try
        {
            // Mint the pet on the Tezos blockchain
            var transactionHash = await _tezosService.MintPet(pet);

            // If we reach here, the minting was successful and confirmed
            pet.UserId = currentUserId;
            pet.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updatedPetDto = pet.ToPetDto();
            await _cacheService.SetPetAsync(updatedPetDto);

            return Ok(new { pet = updatedPetDto, transactionHash });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error adopting pet {id}");
            return StatusCode(500, "An error occurred while adopting the pet. Please try again later.");
        }
    }

    [HttpPatch("{id}/interact")]
    public async Task<IActionResult> Interact(int id, [FromBody] InteractPetDto interactPetDto)
    {
        var cachedPet = await _cacheService.GetPetAsync(id);
        if (cachedPet == null)
        {
            var dbPet = await Pets.FindAsync(id);
            if (dbPet == null)
            {
                return NotFound();
            }
            cachedPet = dbPet.ToPetDto();
        }

        // Get the current user's ID from the JWT token
        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // Check if the current user owns the pet
        if (cachedPet.UserId != currentUserId)
        {
            return Unauthorized("You can only interact with your own pets.");
        }

        // Update pet attributes based on interaction type
        switch (interactPetDto.Type)
        {
            case InteractionType.feed:
                cachedPet.Health += 10;
                cachedPet.Happiness += 5;
                break;
            case InteractionType.play:
                cachedPet.Mood += 15;
                cachedPet.Happiness += 10;
                cachedPet.Health -= 5; // Playing makes the pet a bit tired
                break;
            case InteractionType.train:
                cachedPet.Health += 5;
                cachedPet.Mood -= 10; // Training can be stressful
                cachedPet.Happiness += 15; // But it's rewarding in the long run
                break;
            case InteractionType.groom:
                cachedPet.Health += 15;
                cachedPet.Happiness += 5;
                cachedPet.Mood -= 5; // Some pets might not enjoy grooming
                break;
            case InteractionType.adventure:
                cachedPet.Mood += 20;
                cachedPet.Happiness += 15;
                cachedPet.Health -= 10; // Adventures can be tiring and slightly risky
                break;
            default:
                return BadRequest("Invalid interaction type");
        }

        // Ensure attributes don't go below 0 or above 100
        cachedPet.Health = Math.Clamp(cachedPet.Health, 0, 100);
        cachedPet.Mood = Math.Clamp(cachedPet.Mood, 0, 100);
        cachedPet.Happiness = Math.Clamp(cachedPet.Happiness, 0, 100);

        cachedPet.UpdatedAt = DateTime.UtcNow;

        // Update the cache
        await _cacheService.SetPetAsync(cachedPet);

        // Update the database
        var pet = await Pets.FindAsync(id);
        if (pet != null)
        {
            pet.Health = cachedPet.Health;
            pet.Mood = cachedPet.Mood;
            pet.Happiness = cachedPet.Happiness;
            pet.UpdatedAt = cachedPet.UpdatedAt;
            await _context.SaveChangesAsync();
        }

        return Ok(cachedPet);
    }

    [HttpPost("schedule-feeding")]
    public async Task<IActionResult> ScheduleFeeding([FromBody] ScheduleFeedingDto scheduleFeedingDto)
    {
        var pet = await Pets.FindAsync(scheduleFeedingDto.PetId);
        if (pet == null)
        {
            return NotFound("Pet not found");
        }

        var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (pet.UserId != currentUserId)
        {
            return Unauthorized("You can only schedule feedings for your own pets.");
        }

        if (!TimeSpan.TryParse(scheduleFeedingDto.FeedingTime, out TimeSpan feedingTime))
        {
            return BadRequest("Invalid feeding time format. Please use HH:mm:ss format.");
        }

        // Convert local time to UTC
        var localTime = DateTime.SpecifyKind(DateTime.Now.Date.Add(feedingTime), DateTimeKind.Local);
        var utcTime = localTime.ToUniversalTime();

        var daysOfWeek = scheduleFeedingDto.DaysOfWeek
            .Select(day => Enum.Parse<DayOfWeek>(day, true))
            .ToArray();

        var scheduledTask = new ScheduledTask
        {
            PetId = scheduleFeedingDto.PetId,
            TaskType = "Feed",
            ScheduledTime = utcTime,
            DaysOfWeek = string.Join(",", daysOfWeek)
        };

        _context.ScheduledTasks.Add(scheduledTask);
        await _context.SaveChangesAsync();

        return Ok("Feeding schedule created successfully");
    }

}