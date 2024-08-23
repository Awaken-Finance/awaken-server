
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Tests;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Trade
{
    [Collection(ClusterCollection.Name)]
    public class TokenPriceProviderTests: TradeTestBase
    {
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly ITokenAppService _tokenAppService;
        private readonly IChainAppService _chainAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
        private readonly ChainTestHelper _chainTestHelper;
        private readonly TradePairTestHelper _tradePairTestHelper;
        
        //private readonly ITradePairRepository _tradePairRepository;

        public TokenPriceProviderTests()
        {
            _tokenPriceProvider = GetRequiredService<ITokenPriceProvider>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
            _chainAppService = GetRequiredService<IChainAppService>();
            _tradePairAppService = GetRequiredService<ITradePairAppService>();
            _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
            _chainTestHelper = GetRequiredService<ChainTestHelper>();
            _tradePairTestHelper = GetRequiredService<TradePairTestHelper>();
        }

        [Fact]
        public async Task GetTokenUSDPriceTest()
        {
            var chainBSC = await _chainTestHelper.CreateAsync(new ChainCreateDto
            {
                Name = "BSC"
            });

            var tokenA = await CreateTokenAsync(ChainId, "TOKENA");
            var tokenB = await CreateTokenAsync(ChainId, "TOKENB");
            var tokenMDX = await CreateTokenAsync(ChainId, "MDX");
            
            await _tradePairTestHelper.CreateAsync(new TradePairCreateDto
            {
                ChainId = ChainId,
                Address = "0x06a6FaC8c710e53c4B2c2F96477119dA368",
                FeeRate = 0.5,
                Token0Id = TokenUsdtId,
                Token1Id = TokenEthId
            });
            
            await _tradePairTestHelper.CreateAsync(new TradePairCreateDto
            {
                ChainId = ChainId,
                Address = "0x06a6FaC8c710e53c4B2c2F96477119dA369",
                FeeRate = 0.3,
                Token0Id = TokenEthId,
                Token1Id = tokenMDX
            });

            // BSC
            var price = await _tokenPriceProvider.GetTokenUSDPriceAsync(chainBSC.Id, TokenUsdtSymbol);
            price.ShouldBe(1);

            await _tokenPriceProvider.UpdatePriceAsync(chainBSC.Id, TokenBtcId, TokenUsdtId, 10);
            price = await _tokenPriceProvider.GetTokenUSDPriceAsync(chainBSC.Id, TokenBtcSymbol);
            price.ShouldBe(1);
            price = await _tokenPriceProvider.GetTokenUSDPriceAsync(chainBSC.Id, TokenUsdtSymbol);
            price.ShouldBe(1);

            // ETH
            var priceUsdt = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenUsdtSymbol);
            priceUsdt.ShouldBe(1);
            
            var priceEth = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenEthSymbol);
            priceEth.ShouldBe(1);
            
            var priceBtc = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenBtcSymbol);
            priceBtc.ShouldBe(1);
            
            var priceSashimi = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, "TOKENA");
            priceSashimi.ShouldBe(0);
            
            var priceDef = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, "TOKENB");
            priceDef.ShouldBe(0);

            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenEthId, TokenUsdtId, 10);
            priceEth = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenEthSymbol);
            priceEth.ShouldBe(1);
            
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenBtcId, TokenEthId, 100);
            priceBtc = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenBtcSymbol);
            priceBtc.ShouldBe(1);
            
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, tokenA, TokenEthId, 0.001);
            priceSashimi = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, "TOKENA");
            priceSashimi.ShouldBe(0);
            
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, tokenB, tokenA, 100000);
            priceSashimi = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, "TOKENB");
            priceSashimi.ShouldBe(0);
            
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, TokenEthId, TokenBtcId,0.02);
            priceBtc = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenBtcSymbol);
            priceBtc.ShouldBe(1);
            
            await _tokenPriceProvider.UpdatePriceAsync(ChainId,  TokenUsdtId, TokenEthId,0.5);
            priceEth = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenEthSymbol);
            priceEth.ShouldBe(1);
            
            var tokenUSDC = await CreateTokenAsync(ChainId, "USDC");
            var tokenUNI = await CreateTokenAsync(ChainId, "UNI");
            
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, tokenUSDC,tokenUNI, 10);
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, tokenUSDC,tokenUNI, 10);
            var  priceUNI = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, "UNI");
            priceUNI.ShouldBe(0);
            
            var tokenDAI = await CreateTokenAsync(ChainId, "DAI");
            var tokenSUSHI = await CreateTokenAsync(ChainId, "SUSHI");
            
            await _tokenPriceProvider.UpdatePriceAsync(ChainId, tokenSUSHI, tokenDAI,15);
            var priceSUSHI = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, "SUSHI");
            priceSUSHI.ShouldBe(0);
        }

        [Fact]
        public async Task InitPriceTest()
        {
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(TradePairEthUsdtId, async grain =>
            {
                return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
                {
                    ChainId = ChainId,
                    TradePairId = TradePairEthUsdtId,
                    Timestamp = DateTime.UtcNow,
                    Price = 10,
                    PriceUSD = 10,
                    TVL = 2000,
                    ValueLocked0 = 100,
                    ValueLocked1 = 1000
                    
                });
            });

            var price = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenEthSymbol);
            price.ShouldBe(1);
        }
        
        [Fact]
        public async Task InitPrice_Token0_Root_Test()
        {
            await _tradePairAppService.DeleteManyAsync(new List<Guid>{TradePairBtcEthId, TradePairEthUsdtId});
            
            var pair = await _tradePairTestHelper.CreateAsync(new TradePairCreateDto
            {
                ChainId = ChainId,
                Address = "0x06a6FaC8c710e53c4B2c2F96477119dA368",
                FeeRate = 0.5,
                Token0Id = TokenUsdtId,
                Token1Id = TokenEthId
            });
            
            await _tradePairMarketDataProvider.AddOrUpdateSnapshotAsync(pair.Id, async grain =>
            {
                return await grain.AddOrUpdateSnapshotAsync(new TradePairMarketDataSnapshotGrainDto
                {
                    ChainId = ChainId,
                    TradePairId =pair.Id,
                    Timestamp = DateTime.UtcNow,
                    Price = 0.1,
                    PriceUSD = 0.1,
                    TVL = 2000,
                    ValueLocked0 = 100,
                    ValueLocked1 = 1000
                    
                });
            });

            var price = await _tokenPriceProvider.GetTokenUSDPriceAsync(ChainId, TokenEthSymbol);
            price.ShouldBe(1);
        }

        private async Task<Guid> CreateTokenAsync(string chainId, string symbol)
        {
            var token = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365",
                Decimals = 8,
                Symbol = symbol,
                ChainId = ChainId
            });

            return token.Id;
        }
    }
}