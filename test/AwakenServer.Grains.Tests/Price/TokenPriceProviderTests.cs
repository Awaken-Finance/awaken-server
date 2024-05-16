namespace AwakenServer.Grains.Tests.Price;

using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Xunit;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;

[Collection(ClusterCollection.Name)]
public class TokenPriceProviderTests : AwakenServerGrainTestBase
{
    private readonly ITokenPriceProvider _tokenPriceProvider;
    
    public TokenPriceProviderTests()
    {
        _tokenPriceProvider = GetRequiredService<ITokenPriceProvider>();
    }
    
    [Fact]
    public async Task RetryTest()
    {
        var price = await _tokenPriceProvider.GetHistoryPriceAsync("NO-PRICE", DateTime.Now.ToString("dd-MM-yyyy"));
        price.ShouldBe(0);
    }

}