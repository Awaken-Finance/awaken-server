using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Asset;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Provider;

public interface IGraphQLProvider
{
    public Task<TradePairInfoDtoPageResultDto> GetTradePairInfoListAsync(GetTradePairsInfoInput input);
    public Task<List<LiquidityRecordDto>> GetLiquidRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    public Task<List<SwapRecordDto>> GetSwapRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    public Task<List<SyncRecordDto>> GetSyncRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    public Task<LiquidityRecordPageResult> QueryLiquidityRecordAsync(GetLiquidityRecordIndexInput input);
    public Task<UserLiquidityPageResultDto> QueryUserLiquidityAsync(GetUserLiquidityInput input);
    public Task<List<UserTokenDto>> GetUserTokensAsync(string chainId, string address);
    public Task<long> GetIndexBlockHeightAsync(string chainId);
    public Task<long> GetLastEndHeightAsync(string chainId, string type);
    public Task SetLastEndHeightAsync(string chainId, string type, long height);
}