using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos;

public class GetLimitOrdersInput : PagedAndSortedResultRequestDto
{
    [Required] 
    public string MakerAddress { get; set; }
    public int LimitOrderStatus { get; set; } = 0;
    public string TokenSymbol { get; set; }
}

public enum LimitOrderStatus
{
    Committed = 1,
    PartiallyFilling = 2,
    FullFilled = 3,
    Cancelled = 4,
    Expired = 5,
    Revoked = 6
}