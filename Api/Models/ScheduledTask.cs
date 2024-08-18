using System.ComponentModel.DataAnnotations;

namespace DigipetApi.Api.Models;

public class ScheduledTask
{
    [Key]
    public int TaskId { get; set; }
    public int PetId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public string DaysOfWeek { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;

    // Navigation property
    public Pet? Pet { get; set; }
}
