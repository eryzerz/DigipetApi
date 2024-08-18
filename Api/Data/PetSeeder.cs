using DigipetApi.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DigipetApi.Api.Data;

public static class PetSeeder
{
    public static void SeedPets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pet>().HasData(
            new Pet
            {
                PetId = 1,
                Name = "Buddy",
                Species = "dogs",
                Type = "labrador",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 2,
                Name = "Whiskers",
                Species = "cats",
                Type = "persian",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 3,
                Name = "Tweety",
                Species = "birds",
                Type = "canary",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 4,
                Name = "Rex",
                Species = "dogs",
                Type = "german shepherd",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 5,
                Name = "Fluffy",
                Species = "cats",
                Type = "maine coon",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 6,
                Name = "Polly",
                Species = "birds",
                Type = "parrot",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 7,
                Name = "Spike",
                Species = "dogs",
                Type = "bulldog",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 8,
                Name = "Mittens",
                Species = "cats",
                Type = "siamese",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 9,
                Name = "Chirpy",
                Species = "birds",
                Type = "finch",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 10,
                Name = "Max",
                Species = "dogs",
                Type = "golden retriever",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 11,
                Name = "Luna",
                Species = "cats",
                Type = "russian blue",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 12,
                Name = "Kiwi",
                Species = "birds",
                Type = "budgerigar",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Pet
            {
                PetId = 13,
                Name = "Rocky",
                Species = "dogs",
                Type = "rottweiler",
                Health = 100,
                Mood = 100,
                Happiness = 100,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
    }
}