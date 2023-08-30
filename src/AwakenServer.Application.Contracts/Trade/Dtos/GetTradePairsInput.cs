using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class GetTradePairsInput : PagedAndSortedResultRequestDto
    {
        [Required]
        public string ChainId { get; set; }
        public string Address { get; set; }
        public Guid? Token0Id { get; set; }
        public Guid? Token1Id { get; set; }
        public string Token0Symbol { get; set; }
        public string Token1Symbol { get; set; }
        public double FeeRate { get; set; }
        public string SearchTokenSymbol { get; set; }
        public string TokenSymbol { get; set; }
        public TradePairPage Page { get; set; }
        public TradePairFeature TradePairFeature { get; set; }

        public GetTradePairsInput()
        {
        }
    }
}