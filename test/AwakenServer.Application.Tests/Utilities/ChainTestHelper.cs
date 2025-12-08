using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Grains.Grain.Chain;
using Orleans;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

public class ChainTestHelper
{
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public ChainTestHelper(
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<ChainDto> CreateAsync(ChainCreateDto input)
    {
        var chain = _objectMapper.Map<ChainCreateDto, Chain>(input);
        chain.Id = string.IsNullOrEmpty(chain.Id) ? Guid.NewGuid().ToString() : chain.Id;

        var chainGrain = _clusterClient.GetGrain<IChainGrain>(chain.Id);
        await chainGrain.AddChainAsync(_objectMapper.Map<Chain, ChainGrainDto>(chain));

        await _distributedEventBus.PublishAsync(_objectMapper.Map<Chain, NewChainEvent>(chain));
        return _objectMapper.Map<Chain, ChainDto>(chain);
    }
}