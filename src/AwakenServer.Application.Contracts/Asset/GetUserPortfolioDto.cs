using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Asset;

public class GetUserPortfolioDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string Address { get; set; }
    public int ShowCount { get; set; } = 5;
}