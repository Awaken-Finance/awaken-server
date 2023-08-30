using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Chains
{
    public class ChainDto : EntityDto<string>
    {
        public string Name { get; set; }
        public int BlocksPerDay { get; set; }
        public long LatestBlockHeight { get; set; }

        public int AElfChainId { get; set; }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Name) && AElfChainId == 0;
        }
    }
}