using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Nethereum.Util;
using Orleans;

namespace AwakenServer.Grains.Grain.SwapTokenPath;

public interface ITokenPathGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenPathResultGrainDto>> GetPathAsync(GetTokenPathGrainDto dto);
    Task<GrainResultDto<TokenPathResultGrainDto>> GetCachedPathAsync(GetTokenPathGrainDto dto);
    Task<GrainResultDto> SetGraphAsync(GraphDto dto);
    Task<GrainResultDto> ResetCacheAsync();
}