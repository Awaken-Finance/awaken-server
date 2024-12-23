using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.State.Price;
using AwakenServer.Trade.Dtos;
using Serilog;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Grains.Grain.Price;

public class ChainTradePairsGrain : Grain<ChainTradePairsState>, IChainTradePairsGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger _logger;


    public ChainTradePairsGrain(IObjectMapper objectMapper,
        IClusterClient clusterClient)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _logger = Log.ForContext<ChainTradePairsGrain>();
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public async Task<GrainResultDto<ChainTradePairsGrainDto>> AddOrUpdateAsync(ChainTradePairsGrainDto dto)
    {
        State.TradePairs[dto.TradePairAddress] = dto.TradePairGrainId;
        return new GrainResultDto<ChainTradePairsGrainDto>()
        {
            Success = true,
            Data = dto
        };
    }

    public async Task<GrainResultDto<List<TradePairGrainDto>>> GetAsync()
    {
        var data = new List<TradePairGrainDto>();
        foreach (var tradePair in State.TradePairs)
        {
            var grain = _clusterClient.GetGrain<ITradePairGrain>(tradePair.Value);
            var result = await grain.GetAsync();
            if (!result.Success)
            {
                _logger.Error($"get trade pair grain id: {tradePair.Value} failed.");
            }
            data.Add(result.Data);
        }

        return new GrainResultDto<List<TradePairGrainDto>>()
        {
            Success = true,
            Data = data
        };
    }

    public async Task<GrainResultDto<List<TradePairGrainDto>>> GetAsync(IEnumerable<string> addresses)
    {
        var data = new List<TradePairGrainDto>();
        foreach (var address in addresses)
        {
            if (State.TradePairs.ContainsKey(address))
            {
                var grain = _clusterClient.GetGrain<ITradePairGrain>(State.TradePairs[address]);
                var result = await grain.GetAsync();
                if (!result.Success)
                {
                    _logger.Error($"get trade pair grain id: {State.TradePairs[address]} failed.");
                }
                data.Add(result.Data);
            }
        }
        
        return new GrainResultDto<List<TradePairGrainDto>>()
        {
            Success = true,
            Data = data
        };
    }
    
    public async Task<GrainResultDto<TradePairGrainDto>> GetAsync(string address)
    {
        if (!State.TradePairs.ContainsKey(address))
        {
            return null;
        }
        
        var grain = _clusterClient.GetGrain<ITradePairGrain>(State.TradePairs[address]);
        var result = await grain.GetAsync();
        if (!result.Success)
        {
            _logger.Error($"get trade pair grain id: {State.TradePairs[address]} failed.");
        }

        return new GrainResultDto<TradePairGrainDto>()
        {
            Success = true,
            Data = result.Data
        };
    }

}