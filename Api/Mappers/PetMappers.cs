using DigipetApi.Api.Dtos.Pet;
using DigipetApi.Api.Models;

namespace DigipetApi.Api.Mappers;

public static class PetMappers
{
    public static PetDto ToPetDto(this Pet petModel)
    {
        return new PetDto
        {
            PetId = petModel.PetId,
            UserId = petModel.UserId,
            Name = petModel.Name,
            Species = petModel.Species,
            Type = petModel.Type,
            Health = petModel.Health,
            Mood = petModel.Mood,
            Happiness = petModel.Happiness,
            CreatedAt = petModel.CreatedAt,
            UpdatedAt = petModel.UpdatedAt,
        };
    }
}