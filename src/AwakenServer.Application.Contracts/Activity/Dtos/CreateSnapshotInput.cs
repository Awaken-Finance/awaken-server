using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Activity.Dtos;

public class CreateSnapshotInput
{
    [Required] public long ExecuteTime { get; set; }
}