using DigipetApi.Api.Dtos.Pet;

namespace DigipetApi.Api.Interfaces;

public interface IPetCacheService
{
    Task<PetDto?> GetPetAsync(int petId);
    Task SetPetAsync(PetDto pet);
    Task RemovePetAsync(int petId);
}