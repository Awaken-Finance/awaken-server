using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Chains
{
    public interface IChainAppService
    {
        Task<ChainDto> GetChainAsync(string chainId);

        Task<ListResultDto<ChainDto>> GetListAsync(GetChainInput getChainInput);

        Task<ChainDto> GetByNameCacheAsync(string name);

        Task<ChainDto> GetByChainIdCacheAsync(string chainId);
        
        Task<ChainDto> UpdateAsync(ChainUpdateDto input);

        Task<ChainStatusDto> GetChainStatusAsync(string chainId);
    }
}