using System;
using System.Threading.Tasks;
using AwakenServer.Grains.Grain.Tokens;
using AwakenServer.Tokens;
using Shouldly;
using Xunit;

namespace AwakenServer.Grains.Tests.Tokens;

[Collection(ClusterCollection.Name)]
public class TokenTests:AwakenServerGrainTestBase
{
    [Fact]
    public async Task AddTokenTest()
    {
        var chainId = Guid.NewGuid().ToString();
        var tokenELF = new TokenCreateDto()
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            Address = "xxxxxxx",
            Symbol = "ELF",
        };
        var tokenBTC = new TokenCreateDto()
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            Address = "xxxxxxx",
            Symbol = "BTC",
        };
        var grain = Cluster.Client.GetGrain<ITokenInfoGrain>(tokenELF.Symbol);
        var createResultDto = await grain.CreateAsync(tokenELF);
        var createResult = createResultDto.Data;
        
        createResult.Symbol.ShouldBe("ELF");
        createResult.ChainId.ShouldBe(tokenELF.ChainId);

        var tokenResultDto = await grain.GetAsync();
        var tokenResult = tokenResultDto.Data;
        tokenResult.Symbol.ShouldBe("ELF");
        tokenResult.Id.ShouldBe(createResult.Id);
        
        tokenResultDto = await grain.GetAsync();
        tokenResultDto.Success.ShouldBeTrue();
        
        var grain1 = Cluster.Client.GetGrain<ITokenInfoGrain>("xxx");
        var emptyResultDto = await grain1.CreateAsync(new TokenCreateDto());
        emptyResultDto.Success.ShouldBeFalse();
    }
}