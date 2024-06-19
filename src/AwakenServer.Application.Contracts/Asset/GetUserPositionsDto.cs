using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Asset;

public class GetUserPositionsDto : PagedAndSortedResultRequestDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string Address { get; set; }
}