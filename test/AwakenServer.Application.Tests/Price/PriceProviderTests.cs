using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class PriceProviderTests : PriceTestBase
    {
        private readonly IPriceProvider _priceProvider;
        private readonly ISTARTokenOptions _istarTokenOptions;

        public PriceProviderTests()
        {
            _priceProvider = GetRequiredService<IPriceProvider>();
            _istarTokenOptions = (GetRequiredService<IOptionsSnapshot<ISTARTokenOptions>>()).Value;
        }

        [Fact(Skip = "no need")]
        public async Task GetPriceTest()
        {
            var btcPrice = await _priceProvider.GetPriceAsync(Symbol.BTC);
            btcPrice.ShouldBeGreaterThan(0);
            btcPrice = await _priceProvider.GetPriceAsync(Symbol.BTC);
            btcPrice.ShouldBeGreaterThan(0);
            var istarPrice = await _priceProvider.GetPriceAsync(Symbol.ISTAR);
            istarPrice.ToString().ShouldBe(_istarTokenOptions.InitPrice);
            var sashimiPrice = await _priceProvider.GetPriceAsync(Symbol.SASHIMI);
            sashimiPrice.ShouldBeGreaterThan(0);
            sashimiPrice = await _priceProvider.GetPriceAsync(Symbol.SASHIMI);
            sashimiPrice.ShouldBeGreaterThan(0);
            var ethPrice = await _priceProvider.GetPriceAsync("ETH");
            ethPrice.ShouldBe(0);
        }
    }
}