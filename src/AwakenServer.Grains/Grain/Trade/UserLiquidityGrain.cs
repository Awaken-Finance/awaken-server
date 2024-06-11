using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;
using Orleans;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace AwakenServer.Grains.Grain.Trade;

public class UserLiquidityGrain : Grain<UserLiquidityState>, IUserLiquidityGrain
{
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UserLiquidityGrain> _logger;


    private const string BTCSymbol = "BTC";
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public UserLiquidityGrain(IClusterClient clusterClient,
        IObjectMapper objectMapper,
        ILogger<UserLiquidityGrain> logger)
    {
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<GrainResultDto<UserLiquidityGrainDto>> AddOrUpdateAsync(UserLiquidityGrainDto dto)
    {
        State.TradePairLiquidities ??= new Dictionary<string, Liquidity>();
        if (!State.TradePairLiquidities.ContainsKey(dto.TradePair.Address))
        {
            State.TradePairLiquidities[dto.TradePair.Address] =
                _objectMapper.Map<UserLiquidityGrainDto, Liquidity>(dto);
        }
        else
        {
            if (dto.Type == LiquidityType.Mint)
            {
                State.TradePairLiquidities[dto.TradePair.Address].LpTokenAmount += (dto.IsRevert ? -dto.LpTokenAmount : dto.LpTokenAmount);
            }
            else
            {
                State.TradePairLiquidities[dto.TradePair.Address].LpTokenAmount -= (dto.IsRevert ? -dto.LpTokenAmount : dto.LpTokenAmount);
            }
        }

        return new GrainResultDto<UserLiquidityGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<Liquidity, UserLiquidityGrainDto>(State.TradePairLiquidities[dto.TradePair.Address])
        };
    }
    
    public async Task<GrainResultDto<List<UserLiquidityGrainDto>>> GetAsync()
    {
        State.TradePairLiquidities ??= new Dictionary<string, Liquidity>();
        var dataList = new List<UserLiquidityGrainDto>();
        foreach (var liquidity in State.TradePairLiquidities)
        {
            var grainDto = new UserLiquidityGrainDto
            {
                Address = liquidity.Value.Address,
                LpTokenAmount = liquidity.Value.LpTokenAmount,
                TradePair = liquidity.Value.TradePair
            };

            _logger.LogInformation(
                $"UserLiquidityGrain: {this.GetPrimaryKeyString()}, " +
                $"user tradePair liquidity: {JsonConvert.SerializeObject(liquidity.Value)}");
            
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(liquidity.Value.TradePair.Id));
            var pairResult = await tradePairGrain.GetAsync();
            var pair = pairResult.Data;
            if (pair == null || pair.TotalSupply == null || pair.TotalSupply == "0")
            {
                continue;
            }
            
            _logger.LogInformation(
                $"UserLiquidityGrain: {this.GetPrimaryKeyString()}, " +
                $"get tradePair from TradePairGrain: {JsonConvert.SerializeObject(pair)}");
            
            var prop = pair.TotalSupply == null || pair.TotalSupply == "0"
                ? 0
                : liquidity.Value.LpTokenAmount / double.Parse(pair.TotalSupply);
            
            grainDto.Token0Amount = pair.Token0.Decimals == 0
                ? Math.Floor(prop / Math.Pow(10, 8) * pair.ValueLocked0).ToString()
                : ((long)(prop * pair.ValueLocked0)).ToDecimalsString(8);
            grainDto.Token1Amount = pair.Token1.Decimals == 0
                ? Math.Floor(prop / Math.Pow(10, 8) * pair.ValueLocked1).ToString()
                : ((long)(prop * pair.ValueLocked1)).ToDecimalsString(8);

            grainDto.AssetUSD = pair.TVL * prop / Math.Pow(10, 8);
            dataList.Add(grainDto);
        }

        return new GrainResultDto<List<UserLiquidityGrainDto>>
        {
            Success = true,
            Data = dataList
        };
    }
    
    public async Task<GrainResultDto<UserAssetGrainDto>> GetAssetAsync()
    {
        
        double asset = 0;
        foreach (var liquidity in State.TradePairLiquidities)
        {
            _logger.LogInformation(
                $"UserLiquidityGrain: {this.GetPrimaryKeyString()}, " +
                $"user tradePair liquidity: {JsonConvert.SerializeObject(liquidity.Value)}");
            
            var tradePairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(liquidity.Value.TradePair.Id));
            var pairResult = await tradePairGrain.GetAsync();
            var pair = pairResult.Data;
            if (pair == null || pair.TotalSupply == null || pair.TotalSupply == "0")
            {
                continue;
            }
            
            _logger.LogInformation(
                $"UserLiquidityGrain: {this.GetPrimaryKeyString()}, " +
                $"get tradePair from TradePairGrain: {JsonConvert.SerializeObject(pair)}");

            var totalSupply = double.Parse(pair.TotalSupply);
            asset += pair.TVL * double.Parse(liquidity.Value.LpTokenAmount.ToDecimalsString(8)) / totalSupply;
        }
        
        _logger.LogInformation($"UserLiquidityGrain GetAssetAsync asset: {asset}");
        
        return new GrainResultDto<UserAssetGrainDto>
        {
            Success = true,
            Data = new UserAssetGrainDto
            {
                AssetUSD = asset
            }
        };
    }
}