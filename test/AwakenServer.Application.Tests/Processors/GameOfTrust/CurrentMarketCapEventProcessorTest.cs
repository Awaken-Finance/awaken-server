using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Constants;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Applications.GameOfTrust
{
    public partial class GameOfTrustAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Current_MarketCap_Event_Test()
        {
            await initPool();
            await DoUpdateCurrentMarkedCapAsync();
        }
        
        private async Task DoUpdateCurrentMarkedCapAsync()
        {
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var totalSupply = 30000;
            var marketCap = 60000;
            var averagePrice = 2;
            await CurrentMarketCapAsync(contractAddress, totalSupply, marketCap, averagePrice);
            var (_, esMarketList) = await _esMarketRepository.GetListAsync();
            var target = esMarketList.Last();
            target.Price.ShouldBe(((BigDecimal)averagePrice/BigInteger.Pow(10,tokenUSD.Decimals)).ToString());
            target.TotalSupply.ShouldBe(((BigDecimal)totalSupply/BigInteger.Pow(10,tokenB.Decimals)).ToString());
            target.MarketCap.ShouldBe(((BigDecimal)marketCap/BigInteger.Pow(10,tokenUSD.Decimals)).ToString());
        }
        
        private async Task CurrentMarketCapAsync(string contractAddress, int totalSupply, int marketCap,
            int averagePrice)
        {
            var currentMarketCapProcessor = GetRequiredService<IEventHandlerTestProcessor<CurrentMarketCapEventDto>>();
            await currentMarketCapProcessor.HandleEventAsync(new CurrentMarketCapEventDto
                {
                    AveragePrice = averagePrice,
                    MarketCap = marketCap,
                    TotalSupply = totalSupply
                },
                GetDefaultEventContext(contractAddress,timestamp:DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)/1000));
        }
    }
}