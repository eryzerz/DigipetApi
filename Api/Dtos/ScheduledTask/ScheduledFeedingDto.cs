using System.ComponentModel.DataAnnotations;

namespace DigipetApi.Api.Dtos.ScheduledTask;

public class ScheduleFeedingDto
{
    [Required]
    public int PetId { get; set; }

    [Required]
    public string FeedingTime { get; set; } = "12:00:00";

    [Required]
    public DayOfWeek[] DaysOfWeek { get; set; } = [];
}