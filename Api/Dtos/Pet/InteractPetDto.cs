namespace DigipetApi.Api.Dtos.Pet;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class InteractPetDto
{
    [Required]
    [InteractionType]
    public string Type { get; set; } = "play";
}

public class InteractionTypeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string type)
        {
            string[] validTypes = { "feed", "play", "train", "groom", "adventure" };
            if (validTypes.Contains(type.ToLower()))
            {
                return ValidationResult.Success;
            }
        }
        return new ValidationResult("Invalid interaction type. Valid types are: feed, play, train, groom, adventure.");
    }
}