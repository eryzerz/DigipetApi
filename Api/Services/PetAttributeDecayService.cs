using DigipetApi.Api.Data;
using DigipetApi.Api.Interfaces;
using DigipetApi.Api.Mappers;
using DigipetApi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigipetApi.Api.Services;

public class PetAttributeDecayService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PetAttributeDecayService> _logger;
    private readonly TimeSpan _delay;

    public PetAttributeDecayService(IServiceProvider serviceProvider, ILogger<PetAttributeDecayService> logger, TimeSpan? delay = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _delay = delay ?? TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PetAttributeDecayService is starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPetAttributeDecay();
            await Task.Delay(_delay, stoppingToken);
        }
    }

    public async Task ProcessPetAttributeDecay()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var cacheService = scope.ServiceProvider.GetRequiredService<IPetCacheService>();

            // Get adopted pets -> Only adopted pets decay
            var adoptedPets = await dbContext.Set<Pet>().Where(p => p.UserId != null).ToListAsync();
            foreach (var pet in adoptedPets)
            {
                pet.Health = Math.Max(0, pet.Health - 1);
                pet.Mood = Math.Max(0, pet.Mood - 2);
                pet.Happiness = Math.Max(0, pet.Happiness - 2);
                pet.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation($"Decreased attributes for adopted pet {pet.PetId}. Health: {pet.Health}, Mood: {pet.Mood}, Happiness: {pet.Happiness}");

                // Update cache
                await cacheService.SetPetAsync(pet.ToPetDto());
            }

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing pet attribute decay");
        }
    }
}