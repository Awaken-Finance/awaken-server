using AwakenServer.Grains.Grain.Route;
using AwakenServer.Tokens;
using AwakenServer.Trade.Index;

namespace AwakenServer.Grains.Tests.Route;

using AwakenServer.Grains.Grain.SwapTokenPath;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

[Collection(ClusterCollection.Name)]
public class RouteGrainTests : AwakenServerGrainTestBase
{
    [Fact]
    public async Task GetPathTest()
    {
        var feeRate1 = 0.03;
        var feeRate2 = 0.005;
        var pairs = new List<TradePairWithToken>()
        {
            new TradePairWithToken
            {
                Token0 = new Token()
                {
                    Symbol = "SGR"
                },
                Token1 = new Token()
                {
                    Symbol = "ELEPHANT-1"
                },
                Address = "0x1",
                FeeRate = feeRate1
            },
            new TradePairWithToken
            {
                Token0 = new Token()
                {
                    Symbol = "SGR"
                },
                Token1 = new Token()
                {
                    Symbol = "ELEPHANT-1"
                },
                Address = "0x2",
                FeeRate = feeRate2
            },
            new TradePairWithToken
            {
                Token0 = new Token()
                {
                    Symbol = "SGR"
                },
                Token1 = new Token()
                {
                    Symbol = "ELF"
                },
                Address = "0x3",
                FeeRate = feeRate1
            },
            new TradePairWithToken
            {
                Token0 = new Token()
                {
                    Symbol = "SGR"
                },
                Token1 = new Token()
                {
                    Symbol = "ELF"
                },
                Address = "0x4",
                FeeRate = feeRate2
            }
        };
        
        var grain = Cluster.Client.GetGrain<IRouteGrain>(GrainIdHelper.GenerateGrainId(ChainId));
        var routesResult = await grain.SearchRoutesAsync(new SearchRoutesGrainDto()
        {
            Relations = pairs,
            ChainId = ChainName,
            SymbolBegin = "ELEPHANT-1",
            SymbolEnd = "ELF",
            MaxDepth = 3
        });
        
        routesResult.Data.Routes.Count.ShouldBe(4);
        routesResult.Data.Routes[1].Tokens.Count.ShouldBe(3);
        routesResult.Data.Routes[1].Tokens[0].Symbol.ShouldBe("ELEPHANT-1");
        routesResult.Data.Routes[1].Tokens[1].Symbol.ShouldBe("SGR");
        routesResult.Data.Routes[1].Tokens[2].Symbol.ShouldBe("ELF");
        routesResult.Data.Routes[1].TradePairs.Count.ShouldBe(2);
        routesResult.Data.Routes[1].TradePairs[0].Address.ShouldBe("0x1");
        routesResult.Data.Routes[1].TradePairs[1].Address.ShouldBe("0x4");
        routesResult.Data.Routes[1].FeeRates.Count.ShouldBe(2);
        routesResult.Data.Routes[1].FeeRates[0].ShouldBe(feeRate1);
        routesResult.Data.Routes[1].FeeRates[1].ShouldBe(feeRate2);
        
        // get from cache
        var cachedPathResult = await grain.GetRoutesAsync(new GetRoutesGrainDto()
        {
            ChainId = ChainName,
            SymbolBegin = "ELEPHANT-1",
            SymbolEnd = "ELF",
            MaxDepth = 3
        });
        cachedPathResult.Success.ShouldBe(true);
        routesResult.Data.Routes.Count.ShouldBe(4);
    }
}