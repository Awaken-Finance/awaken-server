using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Dtos;
using AwakenServer.Trade.Index;
using AwakenServer.Web3;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class FarmPriceAppServiceTests : PriceTestBase
    {
        private readonly IFarmPriceAppService _farmPriceAppService;
        private readonly IWeb3Provider _web3Provider;
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;

        public FarmPriceAppServiceTests()
        {
            _farmPriceAppService = GetRequiredService<IFarmPriceAppService>();
            _web3Provider = GetRequiredService<IWeb3Provider>();
            _tradePairIndexRepository =
                GetRequiredService<INESTRepository<TradePair, Guid>>();
        }

        [Fact(Skip = "no need")]
        public async Task GetPricesTest()
        {
            var tokenAddresses = new[] {"0xGEther", "0xAbtc", "0xLpToken", "0xOtherLPToken"};
            var listDto = await _farmPriceAppService.GetPricesAsync(new GetFarmTokenPriceInput
            {
                ChainId = ChainId,
                TokenAddresses = tokenAddresses
            });
            listDto.Count.ShouldBe(2);
            listDto.ShouldAllBe(l=>l.Price == "0");

            var lendingTokenPrices = await CreateLendingTokenPrices();
            var otherLpToken = await CreateOtherLpTokenAsync();
            var tradePair = new TradePair
            {
                Address = "0xLpToken",
                Id = Guid.NewGuid(),
                Token0 = TokenBtc,
                Token1 = TokenEth,
                TVL = 1000,
                TotalSupply = "10",
                ChainId = ChainId
            };
            await _tradePairIndexRepository.AddAsync(tradePair);
            
            listDto = await _farmPriceAppService.GetPricesAsync(new GetFarmTokenPriceInput
            {
                ChainId = ChainId,
                TokenAddresses = tokenAddresses
            });
            listDto.Count.ShouldBe(4);
            var farmToken=listDto.First(d => d.ChainId == ChainId && d.TokenAddress == tokenAddresses[0]);
            farmToken.Price.ShouldBe(
                (BigDecimal.Parse(lendingTokenPrices.First(p => p.TokenId == TokenEthId).Price) *
                 await _web3Provider.GetGTokenExchangeRateAsync("", "ETH") / BigInteger.Pow(10, 10)).ToString());
            farmToken.ChainId.ShouldBe(ChainId);
            farmToken.TokenAddress.ShouldBe(tokenAddresses[0]);
            
            farmToken = listDto.First(d => d.ChainId == ChainId && d.TokenAddress == tokenAddresses[1]);
            farmToken.Price.ShouldBe(
                (BigDecimal.Parse(lendingTokenPrices.First(p => p.TokenId == TokenBtcId).Price) *
                 await _web3Provider.GetATokenExchangeRateAsync("", "BTC", "")).ToString());
            farmToken.ChainId.ShouldBe(ChainId);
            farmToken.TokenAddress.ShouldBe(tokenAddresses[1]);
            
            farmToken = listDto.First(d => d.ChainId == ChainId && d.TokenAddress == tokenAddresses[2]);
            farmToken.Price.ShouldBe((tradePair.TVL / double.Parse(tradePair.TotalSupply)).ToStringInvariant());
            farmToken.ChainId.ShouldBe(ChainId);
            farmToken.TokenAddress.ShouldBe(tokenAddresses[2]);
            
            var reserve0InUSD =
                BigDecimal.Parse(lendingTokenPrices.First(p => p.TokenId == otherLpToken.Token0Id).Price) *
                BigDecimal.Parse(otherLpToken.Reserve0);
            var reserve1InUSD =
                BigDecimal.Parse(lendingTokenPrices.First(p => p.TokenId == otherLpToken.Token1Id).Price) *
                BigDecimal.Parse(otherLpToken.Reserve1);
            farmToken =  listDto.First(d=> d.ChainId == ChainId && d.TokenAddress == tokenAddresses[3]);
            farmToken.Price.ShouldBe(((reserve0InUSD + reserve1InUSD) / await _web3Provider.GetTokenTotalSupplyAsync("",farmToken.TokenAddress)).ToString());
            farmToken.ChainId.ShouldBe(ChainId);
            farmToken.TokenAddress.ShouldBe(tokenAddresses[3]);

            try
            {
                await _farmPriceAppService.GetPricesAsync(new GetFarmTokenPriceInput
                {
                    ChainId = ChainId,
                    TokenAddresses = new []{"Null"}
                });
            }
            catch (KeyNotFoundException e)
            {
                e.Message.ShouldBe("Invalid Farm token");
            }
        }
    }
}