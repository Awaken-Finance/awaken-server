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
        var grain = Cluster.Client.GetGrain<ITokenStateGrain>(tokenELF.Id);
        var createResultDto = await grain.CreateAsync(tokenELF);
        var createResult = createResultDto.Data;
        
        createResult.Symbol.ShouldBe("ELF");
        createResult.ChainId.ShouldBe(tokenELF.ChainId);

        var tokenResultDto = await grain.GetByIdAsync(createResult.Id);
        var tokenResult = tokenResultDto.Data;
        tokenResult.Symbol.ShouldBe("ELF");
        tokenResult.Id.ShouldBe(createResult.Id);
        
        tokenResultDto = await grain.GetByIdAsync(Guid.Empty);
        tokenResultDto.Success.ShouldBeFalse();
        
        var grain1 = Cluster.Client.GetGrain<ITokenStateGrain>(Guid.Empty);
        var emptyResultDto = await grain1.CreateAsync(new TokenCreateDto());
        emptyResultDto.Success.ShouldBeFalse();
    }
}