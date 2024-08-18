namespace DigipetApi.Api.Models;

public class Pet
{
    public int PetId { get; set; }
    public int? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty; // e.g "dogs", "cats", "birds"
    public string Type { get; set; } = string.Empty; // e.g "poodle", "corgi", "persian", "scottish", "canary", "pigeon"
    public int Health { get; set; } = 100;
    public int Mood { get; set; } = 100;
    public int Happiness { get; set; } = 100;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
