using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos;

public class GetLimitOrdersInput : PagedAndSortedResultRequestDto
{
    [Required] 
    public string MakerAddress { get; set; }
    public LimitOrderStatus LimitOrderStatus { get; set; } = 0;
}

public enum LimitOrderStatus
{
    Committed = 1,
    PartiallyFilling = 2,
    FullFilled = 3,
    Cancelled = 4,
    Epired = 5,
    Revoked = 6
}