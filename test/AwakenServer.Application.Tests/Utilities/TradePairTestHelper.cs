using System;
using System.Threading.Tasks;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using Orleans;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using TradePairIndex = AwakenServer.Trade.Index.TradePair;

public class TradePairTestHelper
{
    private readonly ITokenAppService _tokenAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public TradePairTestHelper(
        ITokenAppService tokenAppService,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus)
    {
        _tokenAppService = tokenAppService;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<TradePairDto> CreateAsync(TradePairCreateDto input)
    {
        if (input.Id == Guid.Empty)
        {
            input.Id = Guid.NewGuid();
        }

        var token0 = await _tokenAppService.GetAsync(new GetTokenInput()
        {
            ChainId = input.ChainId,
            Symbol = input.Token0Symbol
        });
        var token1 = await _tokenAppService.GetAsync(new GetTokenInput()
        {
            ChainId = input.ChainId,
            Symbol = input.Token1Symbol
        });
        var tradePairInfo = _objectMapper.Map<TradePairCreateDto, TradePairInfoIndex>(input);
        tradePairInfo.Token0Symbol = token0.Symbol;
        tradePairInfo.Token1Symbol = token1.Symbol;

        var tradePairGrainDto = _objectMapper.Map<TradePairCreateDto, TradePairGrainDto>(input);
        tradePairGrainDto.Token0 = token0;
        tradePairGrainDto.Token1 = token1;

        var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(tradePairInfo.Id));
        await grain.AddOrUpdateAsync(tradePairGrainDto);

        var chainTradePairsGrain = _clusterClient.GetGrain<IChainTradePairsGrain>(input.ChainId);
        await chainTradePairsGrain.AddOrUpdateAsync(new ChainTradePairsGrainDto()
        {
            TradePairAddress = input.Address,
            TradePairGrainId = grain.GetPrimaryKeyString()
        });

        var index = _objectMapper.Map<TradePairCreateDto, TradePairIndex>(input);
        index.Token0 = _objectMapper.Map<TokenDto, Token>(token0);
        index.Token1 = _objectMapper.Map<TokenDto, Token>(token1);

        await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairInfoEto>(
            _objectMapper.Map<TradePairInfoIndex, TradePairInfoEto>(tradePairInfo)
        ));
        await _distributedEventBus.PublishAsync(new EntityCreatedEto<TradePairEto>(
            _objectMapper.Map<TradePairIndex, TradePairEto>(index)
        ));

        return _objectMapper.Map<TradePairInfoIndex, TradePairDto>(tradePairInfo);
    }
}
