using System;
using System.Threading.Tasks;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Orleans.TestingHost;
using Shouldly;

namespace AwakenServer.Trade
{
    public class TradeTestBase: AwakenServerTestBase<TradeTestModule>
    {
        protected string ChainId { get; }
        protected string ChainName { get; }
        protected Guid TradePairEthUsdtId { get; }
        protected Guid TradePairBtcEthId { get; }
        
        protected Guid TokenUsdtId { get; }
        protected string TokenUsdtSymbol { get; }
        protected Guid TokenEthId { get; }
        protected string TokenEthSymbol { get; }
        protected Guid TokenBtcId { get; }
        protected string TokenBtcSymbol { get; }
        
        protected ITradePairAppService TradePairAppService;
        protected ITokenAppService TokenAppService;
        protected readonly TestCluster Cluster;
        
        protected TradeTestBase()
        {
            var environmentProvider = GetRequiredService<TestEnvironmentProvider>();
            TradePairAppService = GetRequiredService<ITradePairAppService>();
            TokenAppService = GetRequiredService<ITokenAppService>();
            Cluster = GetRequiredService<ClusterFixture>().Cluster;

            ChainId = environmentProvider.EthChainId;
            ChainName = environmentProvider.EthChainName;
            TradePairEthUsdtId = environmentProvider.TradePairEthUsdtId;
            TradePairBtcEthId = environmentProvider.TradePairBtcEthId;
            TokenUsdtId = environmentProvider.TokenUsdtId;
            TokenUsdtSymbol = environmentProvider.TokenUsdtSymbol;
            TokenEthId = environmentProvider.TokenEthId;
            TokenEthSymbol = environmentProvider.TokenEthSymbol;
            TokenUsdtSymbol = environmentProvider.TokenUsdtSymbol;
            TokenBtcId = environmentProvider.TokenBtcId;
            TokenBtcSymbol = environmentProvider.TokenBtcSymbol;
        }

        protected async Task CheckTradePairAsync(Guid tradePairId, TradePairWithTokenDto tradePairWithToken)
        {
            var pair = await TradePairAppService.GetAsync(tradePairId);
            tradePairWithToken.Id.ShouldBe(pair.Id);
            tradePairWithToken.Address.ShouldBe(pair.Address);
            tradePairWithToken.FeeRate.ShouldBe(pair.FeeRate);
            CheckToken(tradePairWithToken.Token0,pair.Token0);
            CheckToken(tradePairWithToken.Token1,pair.Token1);
        }

        protected void CheckToken(TokenDto actual, TokenDto expected)
        {
            actual.Id.ShouldBe(expected.Id);
            actual.Address.ShouldBe(expected.Address);
            actual.Symbol.ShouldBe(expected.Symbol);
            actual.Decimals.ShouldBe(expected.Decimals);
        }
    }
}