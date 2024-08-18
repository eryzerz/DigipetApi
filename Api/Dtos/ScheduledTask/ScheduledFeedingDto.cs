using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace DigipetApi.Api.Dtos.ScheduledTask;

public class ScheduleFeedingDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "PetId must be a positive integer.")]
    [DefaultValue(1)]
    public required int PetId { get; set; }

    [Required]
    [RegularExpression(@"^([01]\d|2[0-3]):([0-5]\d):([0-5]\d)$", ErrorMessage = "FeedingTime must be in the format HH:mm:ss")]
    [DefaultValue("12:00:00")]
    public required string FeedingTime { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one day of the week must be selected")]
    [DefaultValue(new[] { "Monday", "Wednesday", "Friday" })]
    public required string[] DaysOfWeek { get; set; }
}