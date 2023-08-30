using System.Threading.Tasks;
using AwakenServer.Tokens;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Xunit;

namespace AwakenServer.Chains;

public class ChainAppServiceTests : ChainTestBase
{
    private readonly IChainAppService _chainAppService;

    public ChainAppServiceTests()
    {
        _chainAppService = GetRequiredService<IChainAppService>();
    }

    [Fact]
    public async Task GetByChainNameCacheTest()
    {
        var chain1 = await _chainAppService.GetByNameCacheAsync(ChainName);
        var chain2 = await _chainAppService.GetByNameCacheAsync(ChainName);
        chain1.Id.ShouldBe(chain2.Id);
    }
    //GetChainAsync
    [Fact]
    public async Task GetChainAsyncTest()
    {
        var chain1 = await _chainAppService.GetChainAsync(ChainName);
        chain1.Name.ShouldBe(ChainName);
        
        chain1 = await _chainAppService.GetByChainIdCacheAsync(chain1.AElfChainId.ToString());
        chain1.Name.ShouldBe(ChainName);
    }
    
    [Fact]
    public async Task GetListAsyncTest()
    {
        var chain1 = await _chainAppService.GetChainAsync(ChainName);
        chain1.Name.ShouldBe(ChainName);
        var chains = await _chainAppService.GetListAsync(new GetChainInput());
        chains.Items.Count.ShouldBe(2);

        chains = await _chainAppService.GetListAsync(new GetChainInput
        {
            IsNeedBlockHeight = true
        });
        chains.Items.Count.ShouldBe(2);
        
    }
    
    [Fact]
    public async Task UpdateAsyncTest()
    {
        var chain1 = await _chainAppService.GetChainAsync(ChainName);
        chain1.Name.ShouldBe(ChainName);
        chain1.LatestBlockHeight.ShouldBe(0);
        chain1 = await _chainAppService.UpdateAsync(new ChainUpdateDto
        {
            Id = chain1.Id,
            AElfChainId = 2,
            Name = ChainName,
            LatestBlockHeight = 100,
        });
        chain1.LatestBlockHeight.ShouldBe(100);
        chain1.AElfChainId = 2;
        
        chain1 = await _chainAppService.UpdateAsync(new ChainUpdateDto
        {
            Id = chain1.Id,
            AElfChainId = 2,
            LatestBlockHeight = 101,
            LatestBlockHeightExpireMs = 1000,
        });
        chain1.LatestBlockHeight.ShouldBe(101);
    }
    
    [Fact]
    public async Task GetChainStatusAsyncTest()
    {
        var chain1 = await _chainAppService.GetChainStatusAsync(ChainName);
        chain1.LatestBlockHeight.ShouldBe(0);
    }

    [Fact]
    public async Task GetBlockchainProvider()
    {
        var factory = GetRequiredService<IBlockchainClientProviderFactory>();
        var factory1 = factory.GetBlockChainClientProvider("Ethereum");
        factory1.ChainType.ShouldBe("Ethereum");
        factory1.GetBlockNumberAsync("Ethereum").Result.ShouldBe(1000);
        factory1.GetTokenInfoAsync("Ethereum", "0x123", "ETH").Result.ShouldBeOfType<TokenDto>();
        var factory2 = factory.GetBlockChainClientProvider("AElfMock");
        factory2.ChainType.ShouldBe("AElfMock");
        factory2.GetBlockNumberAsync("AElfMock").Result.ShouldBe(1000);
        factory2.GetTokenInfoAsync( "AElfMock", "0x123", "ELF").Result.ShouldBeOfType<TokenDto>();
        var factory3 = factory.GetBlockChainClientProvider("AElf");
        factory3.ChainType.ShouldBe("AElf");
        factory3.GetBlockNumberAsync("AElf").Result.ShouldBe(1000);
        factory3.GetTokenInfoAsync("AElf", "0x123", "ELF").Result.ShouldBeOfType<TokenDto>();
    }
}