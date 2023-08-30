using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Trade.Dtos;
using JetBrains.Annotations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    public interface ITradePairAppService : IApplicationService
    {
        Task<PagedResultDto<TradePairIndexDto>> GetListAsync(GetTradePairsInput input);
        
        Task<TradePairIndexDto> GetAsync(Guid id);
        Task<TradePairIndexDto> GetByAddressAsync(Guid id, [CanBeNull] string address);

        Task<TradePairDto> GetTradePairInfoAsync(Guid id);

        Task<TradePairIndexDto> GetTradePairAsync(string chainId, string address);

        Task<ListResultDto<TradePairIndexDto>> GetByIdsAsync(GetTradePairByIdsInput input);

        Task<TokenListDto> GetTokenListAsync(GetTokenListInput input);

        Task<TradePairDto> GetByAddressAsync(string chainName, [CanBeNull] string address);
        Task<List<TradePairIndexDto>> GetListAsync(string chainId, IEnumerable<string> addresses);
        Task<List<TradePairDto>> GetTradePairInfoListAsync(GetTradePairsInfoInput input);
        Task<TradePairDto> CreateAsync(TradePairCreateDto input);
        
        Task UpdateLiquidityAsync(LiquidityUpdateDto input);
        Task UpdateLiquidityAsync(SyncRecordDto dto);

        Task UpdateTradePairAsync(Guid id);
        Task DeleteManyAsync(List<Guid> ids);

        Task SyncTokenAsync(TradePairInfoDto pair, ChainDto chain);
            
        Task SyncPairAsync(TradePairInfoDto pair, ChainDto chain);
    }
}