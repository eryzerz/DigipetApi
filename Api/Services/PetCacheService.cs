using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using DigipetApi.Api.Dtos.Pet;
using DigipetApi.Api.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigipetApi.Api.Services;

public class PetCacheService : IPetCacheService
{
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _options;
    private readonly ILogger<PetCacheService> _logger;

    public PetCacheService(IDistributedCache cache, ILogger<PetCacheService> logger)
    {
        _cache = cache;
        _options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };
        _logger = logger;
    }

    public async Task<PetDto?> GetPetAsync(int petId)
    {
        var cachedPetBytes = await _cache.GetAsync($"pet_{petId}");
        if (cachedPetBytes != null)
        {
            _logger.LogInformation($"Cache hit for pet_{petId}");
            var cachedPetJson = System.Text.Encoding.UTF8.GetString(cachedPetBytes);
            return JsonSerializer.Deserialize<PetDto>(cachedPetJson);
        }
        _logger.LogInformation($"Cache miss for pet_{petId}");
        return null;
    }

    public async Task SetPetAsync(PetDto pet)
    {
        var petJson = JsonSerializer.Serialize(pet);
        var petBytes = System.Text.Encoding.UTF8.GetBytes(petJson);
        await _cache.SetAsync($"pet_{pet.PetId}", petBytes, _options);
    }

    public async Task RemovePetAsync(int petId)
    {
        await _cache.RemoveAsync($"pet_{petId}");
    }
}