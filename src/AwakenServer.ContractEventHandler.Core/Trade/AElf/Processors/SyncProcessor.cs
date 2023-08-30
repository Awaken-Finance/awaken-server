using System.Threading.Tasks;
using AElf;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Swap;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.ContractEventHandler.Trade.AElf.Processors
{
    public class SyncProcessor : AElfEventProcessorBase<Sync>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairAppService _tradePairAppService;

        public SyncProcessor(ITokenAppService tokenAppService, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
        }

        protected override async Task HandleEventAsync(Sync eventDetailsEto, EventContext txInfoDto)
        {
            var nodeName = ChainHelper.ConvertChainIdToBase58(txInfoDto.ChainId);
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var pariAddress = eventDetailsEto.Pair.ToBase58();
            var pair = await _tradePairAppService.GetByAddressAsync(chain.Name, pariAddress);
            var token0 = await _tokenAppService.GetAsync(pair.Token0Id);
            var token1 = await _tokenAppService.GetAsync(pair.Token1Id);

            var isReversed = token0.Symbol == eventDetailsEto.SymbolB;
            string token0Amount;
            string token1Amount;
            if (isReversed)
            {
                token0Amount = eventDetailsEto.ReserveB.ToDecimalsString(token0.Decimals);
                token1Amount = eventDetailsEto.ReserveA.ToDecimalsString(token1.Decimals);
            }
            else
            {
                token0Amount = eventDetailsEto.ReserveA.ToDecimalsString(token0.Decimals);
                token1Amount = eventDetailsEto.ReserveB.ToDecimalsString(token1.Decimals);
            }

            await _tradePairAppService.UpdateLiquidityAsync(new LiquidityUpdateDto
            {
                ChainId = chain.Id,
                TradePairId = pair.Id,
                Token0Amount = token0Amount,
                Token1Amount = token1Amount,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(txInfoDto.BlockTime)
            });
        }
    }
}