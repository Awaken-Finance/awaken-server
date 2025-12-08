using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class GetUserLiquidityInput : PagedAndSortedResultRequestDto
    {
        [Required]
        public string ChainId { get; set; }
        [Required]
        public string Address { get; set; }

        public GetUserLiquidityInput()
        {

        }
    }
}