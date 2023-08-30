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
    public class LiquidityAddedProcessor: AElfEventProcessorBase<LiquidityAdded>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ILiquidityAppService _liquidityAppService;

        public LiquidityAddedProcessor(ITokenAppService tokenAppService, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService, ILiquidityAppService liquidityAppService)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
            _liquidityAppService = liquidityAppService;
        }

        protected override async Task HandleEventAsync(LiquidityAdded eventDetailsEto, EventContext txInfoDto)
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
                token0Amount = eventDetailsEto.AmountB.ToDecimalsString(token0.Decimals);
                token1Amount = eventDetailsEto.AmountA.ToDecimalsString(token1.Decimals);
            }
            else
            {
                token0Amount = eventDetailsEto.AmountA.ToDecimalsString(token0.Decimals);
                token1Amount = eventDetailsEto.AmountB.ToDecimalsString(token1.Decimals);
            }
            
            var sender = eventDetailsEto.Sender.ToBase58();
            await _liquidityAppService.CreateAsync(new LiquidityRecordCreateDto
            {
                ChainId = chain.Id,
                TradePairId = pair.Id,
                Address = sender,
                TransactionHash = txInfoDto.TransactionId,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(txInfoDto.BlockTime),
                Type = LiquidityType.Mint,
                Token0Amount = token0Amount,
                Token1Amount = token1Amount,
                LpTokenAmount = eventDetailsEto.LiquidityToken.ToDecimalsString(8),
                Channel = eventDetailsEto.Channel,
                Sender = sender
            });
        }
    }
}