using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Nethereum.Util;
using Orleans;

namespace AwakenServer.Grains.Grain.Route;

public interface IRouteGrain : IGrainWithStringKey
{
    Task<GrainResultDto<RoutesResultGrainDto>> SearchRoutesAsync(SearchRoutesGrainDto dto);
    Task<GrainResultDto<RoutesResultGrainDto>> GetRoutesAsync(GetRoutesGrainDto dto);
    Task<GrainResultDto<long>> ResetCacheAsync();
}