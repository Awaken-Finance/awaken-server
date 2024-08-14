using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
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
    [EnumMember(Value = "COMMITTED")]
    Committed = 1,
    
    [EnumMember(Value = "PARTIALLY_FILLING")]
    PartiallyFilling = 2,
    
    [EnumMember(Value = "FULL_FILLED")]
    FullFilled = 3,
    
    [EnumMember(Value = "CANCELLED")]
    Cancelled = 4,
    
    [EnumMember(Value = "EXPIRED")]
    Expired = 5,
    
    [EnumMember(Value = "REVOKED")]
    Revoked = 6
}