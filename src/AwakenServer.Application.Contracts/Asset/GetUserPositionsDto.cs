using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Asset;

public class GetUserPositionsDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string Address { get; set; }
}