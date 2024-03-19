using System;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Chains;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AwakenServer.Trade.Handlers
{
    public class NewLiquidityHandler : ILocalEventHandler<NewLiquidityRecordEvent>, ITransientDependency
    {
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly IAElfClientProvider _blockchainClientProvider;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ContractsTokenOptions _contractsTokenOptions;
        private readonly ILogger<NewLiquidityHandler> _logger;

        public NewLiquidityHandler(ITradePairMarketDataProvider tradePairMarketDataProvider,
            IAElfClientProvider blockchainClientProvider, ITradePairAppService tradePairAppService,
            IOptions<ContractsTokenOptions> contractsTokenOptions, ILogger<NewLiquidityHandler> logger)
        {
            _tradePairMarketDataProvider = tradePairMarketDataProvider;
            _blockchainClientProvider = blockchainClientProvider;
            _tradePairAppService = tradePairAppService;
            _contractsTokenOptions = contractsTokenOptions.Value;
            _logger = logger;
        }

        public async Task HandleEventAsync(NewLiquidityRecordEvent eventData)
        {
            var lpAmount = BigDecimal.Parse(eventData.LpTokenAmount);
            lpAmount = eventData.Type == LiquidityType.Mint ? lpAmount : -lpAmount;
            var token = await GetTokenInfoAsync(eventData);
            
            var supply = token != null ? token.Supply.ToDecimalsString(token.Decimals) : "";

            _logger.LogInformation("NewLiquidityRecordEvent,supply:{supply}", supply);
            await _tradePairMarketDataProvider.UpdateTotalSupplyAsync(eventData.ChainId, eventData.TradePairId,
                eventData.Timestamp, lpAmount, supply);
        }


        private async Task<TokenInfo> GetTokenInfoAsync(NewLiquidityRecordEvent eventData)
        {
            try
            {
                var tradePairIndexDto = await _tradePairAppService.GetFromGrainAsync(eventData.TradePairId);

                if (tradePairIndexDto == null || !_contractsTokenOptions.Contracts.TryGetValue(
                        tradePairIndexDto.FeeRate.ToString(),
                        out var address))
                {
                    return null;
                }

                var token = await _blockchainClientProvider.GetTokenInfoFromChainAsync(eventData.ChainId, address,
                    TradePairHelper.GetLpToken(tradePairIndexDto.Token0.Symbol, tradePairIndexDto.Token1.Symbol));
                return token;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get token info failed");
                return null;
            }
        }
    }
}