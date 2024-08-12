using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos;

public class GetLimitOrderDetailsInput : PagedAndSortedResultRequestDto
{
    [Required] 
    public long OrderId { get; set; }
}