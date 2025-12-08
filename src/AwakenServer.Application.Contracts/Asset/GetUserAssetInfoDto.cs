using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Asset;

public class GetUserAssetInfoDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string Address { get; set; }
}