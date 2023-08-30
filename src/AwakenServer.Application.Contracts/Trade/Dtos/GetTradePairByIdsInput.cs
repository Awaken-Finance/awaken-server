using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Trade.Dtos
{
    public class GetTradePairByIdsInput:PagedAndSortedResultRequestDto
    {
        [MaxLength(TradePairConst.MaxIdsLength)]
        public List<Guid> Ids { get; set; }
        
        public string SearchTokenSymbol { get; set; }
    }
}