using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Activity.Dtos;

public class ActivityBaseDto
{
    [Required] public int ActivityId { get; set; }
}