using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Provider;
using AwakenServer.Trade;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AwakenServer.Asset;

public class AssetAppServiceTests : TradeTestBase
{
    private readonly MockGraphQLProvider _graphQlProvider;
    private readonly IAssetAppService _assetAppService;
    private readonly AssetShowOptions _assetShowOptions;

    public AssetAppServiceTests()
    {
        _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        _assetAppService = GetRequiredService<IAssetAppService>();
        _assetShowOptions = GetRequiredService<IOptionsSnapshot<AssetShowOptions>>().Value;
    }

    [Fact]
    public async Task GetUserAssetTest()
    {
        var userTokenDto = new UserTokenDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789",
            Symbol = "USDT",
            Balance = 100
        };
        var userTokenDto1 = new UserTokenDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789",
            Symbol = "BTC",
            Balance = 300
        };
        var userTokenDto2 = new UserTokenDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789",
            Symbol = "EOS",
            Balance = 500
        };
        
        _graphQlProvider.AddUserToken(userTokenDto);
        var userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789"
        });
        userAssetInfo.ShowList.Count.ShouldBe(1);
        userAssetInfo.ShowList.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.ShowList.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.ShowList.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.ShowList.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.ShowList.First().Amount.ShouldBe("0.0001");
        userAssetInfo.HiddenList.Count.ShouldBe(0);
        
        _graphQlProvider.AddUserToken(userTokenDto1);
        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789"
        });
        userAssetInfo.ShowList.Count.ShouldBe(2);
        userAssetInfo.ShowList.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.ShowList.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.ShowList.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.ShowList.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.ShowList.First().Amount.ShouldBe("0.0001");
        userAssetInfo.ShowList.Last().ChainId.ShouldBe(userTokenDto1.ChainId);
        userAssetInfo.ShowList.Last().Address.ShouldBe(userTokenDto1.Address);
        userAssetInfo.ShowList.Last().Symbol.ShouldBe(userTokenDto1.Symbol);
        userAssetInfo.ShowList.Last().Balance.ShouldBe(userTokenDto1.Balance);
        userAssetInfo.ShowList.Last().Amount.ShouldBe("0.000003");
        userAssetInfo.HiddenList.Count.ShouldBe(0);
        
        _graphQlProvider.AddUserToken(userTokenDto2);
        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789"
        });
        userAssetInfo.ShowList.Count.ShouldBe(3);
        //userAssetInfo.HiddenList.Count().ShouldBe(1);
        userAssetInfo.ShowList.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.ShowList.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.ShowList.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.ShowList.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.ShowList.First().Amount.ShouldBe("0.0001");
        userAssetInfo.ShowList.Last().ChainId.ShouldBe(userTokenDto2.ChainId);
        userAssetInfo.ShowList.Last().Address.ShouldBe(userTokenDto2.Address);
        userAssetInfo.ShowList.Last().Symbol.ShouldBe(userTokenDto2.Symbol);
        userAssetInfo.ShowList.Last().Balance.ShouldBe(userTokenDto2.Balance);
        userAssetInfo.ShowList.Last().Amount.ShouldBe("0.000005");
        // userAssetInfo.HiddenList.First().ChainId.ShouldBe(userTokenDto2.ChainId);
        // userAssetInfo.HiddenList.First().Symbol.ShouldBe(userTokenDto2.Symbol);
        // userAssetInfo.HiddenList.First().Balance.ShouldBe(userTokenDto2.Balance);
        
        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "Ethereum",
            Address = "0x1234567890"
        });
        userAssetInfo.ShowList.Count.ShouldBe(0);
        userAssetInfo.HiddenList.Count.ShouldBe(0);
    }
}