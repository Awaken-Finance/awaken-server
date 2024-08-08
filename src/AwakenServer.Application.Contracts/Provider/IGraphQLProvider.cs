using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Asset;
using AwakenServer.Common;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Provider;

public interface IGraphQLProvider
{
    public Task<TradePairInfoDtoPageResultDto> GetTradePairInfoListAsync(GetTradePairsInfoInput input);
    public Task<List<LiquidityRecordDto>> GetLiquidRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight, int skipCount, int maxResultCount);
    public Task<List<SwapRecordDto>> GetSwapRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight, int skipCount, int maxResultCount);
    public Task<List<SyncRecordDto>> GetSyncRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight, int skipCount, int maxResultCount);
    public Task<LiquidityRecordPageResult> QueryLiquidityRecordAsync(GetLiquidityRecordIndexInput input);
    public Task<UserLiquidityPageResultDto> QueryUserLiquidityAsync(GetUserLiquidityInput input);
    public Task<List<UserTokenDto>> GetUserTokensAsync(string chainId, string address);
    public Task<long> GetIndexBlockHeightAsync(string chainId);
    public Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType type);
    public Task SetLastEndHeightAsync(string chainId, WorkerBusinessType type, long height);
    public Task<LimitOrderPageResultDto> QueryLimitOrderAsync(GetLimitOrdersInput input);
    public Task<LimitOrderPageResultDto> QueryLimitOrderAsync(GetLimitOrderDetailsInput input);
    
}