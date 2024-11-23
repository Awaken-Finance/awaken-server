using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using AwakenServer.Trade.Dtos;
using Org.BouncyCastle.Crypto.Prng.Drbg;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.Trade
{
    public class AetherLinkTokenPriceProviderTests : AwakenServerAetherLinkApiTestBase
    {
        private readonly AetherLinkTokenPriceProvider _aetherLinkTokenPriceProvider;

        public AetherLinkTokenPriceProviderTests()
        {
            _aetherLinkTokenPriceProvider = GetRequiredService<AetherLinkTokenPriceProvider>();
        }

        [Fact]
        public async Task GetPriceAsyncTest()
        {
            var result = await _aetherLinkTokenPriceProvider.GetPriceAsync("elf-usdt");
            result.ShouldBe(1);
        }
        
        [Fact]
        public async Task GetHistoryPriceAsyncTest()
        {
            var result = await _aetherLinkTokenPriceProvider.GetHistoryPriceAsync("elf-usdt", DateTime.UtcNow.ToString("dd-MM-yyyy"));
            result.ShouldBe(2);
        }
    }
}