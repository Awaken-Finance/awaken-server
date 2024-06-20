using System;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Chains;
using AwakenServer.Grains.Grain.Trade;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Trade.Handlers
{
    public class NewLiquidityHandler : ILocalEventHandler<NewLiquidityRecordEvent>, ITransientDependency
    {
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ILogger<NewLiquidityHandler> _logger;
        private readonly IObjectMapper _objectMapper;
        private readonly IAElfClientProvider _blockchainClientProvider;
        private readonly ContractsTokenOptions _contractsTokenOptions;

        public NewLiquidityHandler(ITradePairMarketDataProvider tradePairMarketDataProvider,
            ITradePairAppService tradePairAppService,
            ILogger<NewLiquidityHandler> logger,
            IObjectMapper objectMapper,
            IAElfClientProvider blockchainClientProvider,
            IOptions<ContractsTokenOptions> contractsTokenOptions)
        {
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _tradePairAppService = tradePairAppService;
            _objectMapper = objectMapper;
            _logger = logger;
            _blockchainClientProvider = blockchainClientProvider;
            _contractsTokenOptions = contractsTokenOptions.Value;
        }

        private async Task<TokenInfo> GetTokenInfoAsync(Guid tradePairId, string chainId)
        {
            try
            {
                var tradePairIndexDto = await _tradePairAppService.GetAsync(tradePairId);
                
                if (tradePairIndexDto == null || !_contractsTokenOptions.Contracts.TryGetValue(
                        tradePairIndexDto.FeeRate.ToString(),
                        out var address))
                {
                    _logger.LogError("GetTokenInfoAsync, Get tradePairIndexDto failed");
                    return null;
                }

                var token = await _blockchainClientProvider.GetTokenInfoFromChainAsync(chainId, address,
                    TradePairHelper.GetLpToken(tradePairIndexDto.Token0.Symbol, tradePairIndexDto.Token1.Symbol));
                _logger.LogInformation($"lp token {TradePairHelper.GetLpToken(tradePairIndexDto.Token0.Symbol, tradePairIndexDto.Token1.Symbol)}, supply {token.Supply}");
                return token;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get token info failed");
                return null;
            }
        }

        
        public async Task HandleEventAsync(NewLiquidityRecordEvent eventData)
        {
            var dto = _objectMapper.Map<NewLiquidityRecordEvent, LiquidityRecordGrainDto>(eventData);
            var token = await GetTokenInfoAsync(eventData.TradePairId, eventData.ChainId);
            dto.TotalSupply = token != null ? token.Supply.ToDecimalsString(token.Decimals) : "0";
            _logger.LogInformation($"handle new liquidity record event get pair {eventData.TradePairId}, supply {dto.TotalSupply}");
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(eventData.TradePairId, async grain =>
            {
                return await grain.UpdateTotalSupplyAsync(dto);
            });
        }
    }
}