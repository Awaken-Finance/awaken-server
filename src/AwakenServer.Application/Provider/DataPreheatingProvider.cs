using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Options;
using Orleans;
using Serilog;
using Volo.Abp.Threading;

namespace AwakenServer.Provider;

public class DataPreheatingProvider
{
    private readonly ITradePairAppService _tradePairAppService;
    private readonly IClusterClient _clusterClient;
    private readonly ChainsInitOptions _chainsOption;
    private readonly ILogger _logger;

    
    public DataPreheatingProvider(ITradePairAppService tradePairAppService, 
        IClusterClient clusterClient,
        IOptions<ChainsInitOptions> chainsOption)
    {
        _tradePairAppService = tradePairAppService;
        _clusterClient = clusterClient;
        _chainsOption = chainsOption.Value;
        _logger = Log.ForContext<DataPreheatingProvider>();
        AsyncHelper.RunSync(async () =>
            await PreloadData());
    }

    public async Task PreloadData()
    {
        foreach (var chain in _chainsOption.Chains)
        {
            var pairs = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = chain.Name,
                MaxResultCount = 1000
            });
            try
            {
                foreach (var pair in pairs.Items)
                {
                    var grain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(pair.Id));
                    await grain.GetAsync();
                }
                _logger.Information($"PreloadData done, chain: {chain.Name}, pair count: {pairs.Items.Count}");
            }
            catch (Exception e)
            {
                _logger.Error($"PreloadData failed, chain: {chain.Name}, {e}");
            }
        }
    }
}
