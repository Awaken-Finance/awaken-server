namespace AwakenServer.Grains.Tests.Path;

using AutoMapper.Internal.Mappers;
using AwakenServer.Grains.Grain.SwapTokenPath;
using AwakenServer.Grains.Grain.Price;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Shouldly;
using Xunit;

[Collection(ClusterCollection.Name)]
public class TokenPathGrainTests : AwakenServerGrainTestBase
{
    [Fact]
    public async Task GetPathTest()
    {
        var feeRate1 = 0.03;
        var feeRate2 = 0.005;
        var pairs = new List<TradePairDto>()
        {
            new TradePairDto
            {
                Token0Symbol = "ELEPHANT-1",
                Token1Symbol = "ELF",
                Address = "0x1",
                FeeRate = feeRate1
            },
            new TradePairDto
            {
                Token0Symbol = "SGR",
                Token1Symbol = "USDT",
                Address = "0x2",
                FeeRate = feeRate1
            },
            new TradePairDto
            {
                Token0Symbol = "ELF",
                Token1Symbol = "USDT",
                Address = "0x3",
                FeeRate = feeRate1
            },
            new TradePairDto
            {
                Token0Symbol = "ELEPHANT-1",
                Token1Symbol = "USDT",
                Address = "0x4",
                FeeRate = feeRate1
            },
            new TradePairDto
            {
                Token0Symbol = "ELEPHANT-1",
                Token1Symbol = "ELF",
                Address = "0x5",
                FeeRate = feeRate2
            },
            new TradePairDto
            {
                Token0Symbol = "SGR",
                Token1Symbol = "USDT",
                Address = "0x6",
                FeeRate = feeRate2
            },
            new TradePairDto
            {
                Token0Symbol = "ELF",
                Token1Symbol = "USDT",
                Address = "0x7",
                FeeRate = feeRate2
            }
        };
        
        var grain = Cluster.Client.GetGrain<ITokenPathGrain>(GrainIdHelper.GenerateGrainId(ChainId));
        await grain.SetGraphAsync(new GraphDto()
        {
            Relations = pairs
        });

        var dto = new GetTokenPathGrainDto()
        {
            ChainId = ChainName,
            StartSymbol = "ELEPHANT-1",
            EndSymbol = "SGR",
            MaxDepth = 3
        };
        
        // first search, save to cache
        var pathResult = await grain.GetPathAsync(dto);
        
        pathResult.Data.Path.Count.ShouldBe(3);
        pathResult.Data.Path[1].FeeRate.ShouldBe(feeRate1);
        pathResult.Data.Path[1].Path.Count.ShouldBe(2);
        pathResult.Data.Path[1].Path[0].Address.ShouldBe("0x4");
        pathResult.Data.Path[1].Path[1].Address.ShouldBe("0x2");
        pathResult.Data.Path[2].FeeRate.ShouldBe(feeRate2);
        pathResult.Data.Path[2].Path.Count.ShouldBe(3);
        pathResult.Data.Path[2].Path[0].Address.ShouldBe("0x5");
        pathResult.Data.Path[2].Path[1].Address.ShouldBe("0x7");
        pathResult.Data.Path[2].Path[2].Address.ShouldBe("0x6");

        // get from cache
        var cachedPathResult = await grain.GetCachedPathAsync(dto);
        cachedPathResult.Success.ShouldBe(true);
        pathResult.Data.Path.Count.ShouldBe(3);
        
        
    }
}