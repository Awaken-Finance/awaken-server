using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.GameOfTrust.DTos
{
    public class GetGameListInput: PagedAndSortedResultRequestDto
    {   
        public string ChainId { get; set; }
        public string DepositTokenSymbol { get; set; }
        public string HarvestTokenSymbol { get; set; }
    }
}