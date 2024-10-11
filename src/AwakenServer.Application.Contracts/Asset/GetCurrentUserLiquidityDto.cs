using System;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Asset;

public class GetCurrentUserLiquidityDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string Address { get; set; }
    [Required] public Guid TradePairId { get; set; }
}