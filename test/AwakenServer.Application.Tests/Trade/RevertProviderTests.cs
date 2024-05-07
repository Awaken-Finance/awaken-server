using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Common;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Trade
{
    public class RevertProviderTests: TradeTestBase
    {
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly ITokenAppService _tokenAppService;
        private readonly IChainAppService _chainAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly IRevertProvider _revertProvider;
        private readonly MockGraphQLProvider _graphQlProvider;

        public RevertProviderTests()
        {
            _tokenPriceProvider = GetRequiredService<ITokenPriceProvider>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
            _chainAppService = GetRequiredService<IChainAppService>();
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _revertProvider = GetRequiredService<IRevertProvider>();
            _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
            _tradeRecordAppService = GetRequiredService<ITradeRecordAppService>();
        }

        [Fact]
        public async Task TestGetNeedDeleteTransactions_SyncAlreadyConfirmedBlock()
        {
            await _graphQlProvider.SetConfirmBlockHeightAsync(2);
            // block 1 is a confirm block, no need add to may revert cache
            await _revertProvider.CheckOrAddUnconfirmedTransaction(0,EventType.SwapEvent, ChainId, 1, "A");
            var needDelete = await _revertProvider.GetNeedDeleteTransactionsAsync(EventType.SwapEvent, ChainId);
            needDelete.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task TestGetNeedDeleteTransactions_PartRevert()
        {
            await _graphQlProvider.SetConfirmBlockHeightAsync(0);
            await _revertProvider.CheckOrAddUnconfirmedTransaction(0,EventType.SwapEvent, ChainId, 1, "A");
            await _revertProvider.CheckOrAddUnconfirmedTransaction(0,EventType.SwapEvent, ChainId, 2, "B");
            await _revertProvider.CheckOrAddUnconfirmedTransaction(0,EventType.SwapEvent, ChainId, 3, "C");
            _graphQlProvider.AddSwapRecord(new SwapRecordDto()
            {
                ChainId = ChainId,
                TransactionHash = "A",
                BlockHeight = 1
            });
            await _graphQlProvider.SetConfirmBlockHeightAsync(2);
            var needDelete = await _revertProvider.GetNeedDeleteTransactionsAsync(EventType.SwapEvent, ChainId);
            needDelete.Count.ShouldBe(1);
            needDelete[0].ShouldBe("B");
        }
        
        [Fact]
        public async Task TestGetNeedDeleteTransactions_NoNeedRevert()
        {
            await _graphQlProvider.SetConfirmBlockHeightAsync(0);
            // block 1 is an unconfirm block, add to may revert cache
            await _revertProvider.CheckOrAddUnconfirmedTransaction(0,EventType.SwapEvent, ChainId, 1, "A");
            _graphQlProvider.AddSwapRecord(new SwapRecordDto()
            {
                ChainId = ChainId,
                TransactionHash = "A",
                BlockHeight = 1
            });
            await _graphQlProvider.SetConfirmBlockHeightAsync(2);
            var needDelete = await _revertProvider.GetNeedDeleteTransactionsAsync(EventType.SwapEvent, ChainId);
            needDelete.Count.ShouldBe(0);
        }

        
        [Fact]
        public async Task TestRevertData()
        {
            await _tradeRecordAppService.CreateAsync(0,new SwapRecordDto
            {
                ChainId = ChainId,
                TransactionHash = "A",
                PairAddress = TradePairEthUsdtAddress,
                SymbolIn = TokenEthSymbol,
                SymbolOut = TokenUsdtSymbol,
                AmountIn = NumberFormatter.WithDecimals(10, 8),
                AmountOut = NumberFormatter.WithDecimals(10, 6)
            });
            
            var tradePair = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainId,
                Token0Symbol = TokenEthSymbol,
                Token1Symbol = TokenUsdtSymbol
            });
            tradePair.Items[0].Volume24h.ShouldBe(10);
            tradePair.Items[0].TradeValue24h.ShouldBe(10);
            tradePair.Items[0].TradeCount24h.ShouldBe(1);
            
            var deleteList = new List<string>();
            deleteList.Add("A");
            await _tradeRecordAppService.DoRevertAsync(ChainId, deleteList);
            
            tradePair = await _tradePairAppService.GetListAsync(new GetTradePairsInput
            {
                ChainId = ChainId,
                Token0Symbol = TokenEthSymbol,
                Token1Symbol = TokenUsdtSymbol
            });
            tradePair.Items[0].Volume24h.ShouldBe(0);
            tradePair.Items[0].TradeValue24h.ShouldBe(0);
            tradePair.Items[0].TradeCount24h.ShouldBe(0);
        }
    }
}