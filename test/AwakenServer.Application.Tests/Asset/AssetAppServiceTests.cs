using System;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Grains.Tests;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Trade;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AwakenServer.Asset;

[Collection(ClusterCollection.Name)]
public class AssetAppServiceTests : TradeTestBase
{
    private readonly MockGraphQLProvider _graphQlProvider;
    private readonly IAssetAppService _assetAppService;
    private readonly AssetShowOptions _assetShowOptions;
    private readonly IPriceAppService _priceAppService;
    private readonly AssetWhenNoTransactionOptions _assetWhenNoTransactionOptions;

    public AssetAppServiceTests()
    {
        _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        _assetAppService = GetRequiredService<IAssetAppService>();
        _assetShowOptions = GetRequiredService<IOptionsSnapshot<AssetShowOptions>>().Value;
        _priceAppService = GetRequiredService<IPriceAppService>();
        _assetWhenNoTransactionOptions = GetRequiredService<IOptionsSnapshot<AssetWhenNoTransactionOptions>>().Value;
    }


    [Fact]
    public async Task TrasactionFeeTest()
    {
        var transactionFeeAsync = await _assetAppService.GetTransactionFeeAsync();

        transactionFeeAsync.TransactionFee.ShouldBe(1);
    }

    [Fact]
    public async Task GetCacheUserAssetTest()
    {
        var userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "CAElf",
            Address = "0x123456789"
        });

        userAssetInfo.ShowList.Count.ShouldBe(10);

        var userAssetInfo1 = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "CAElf",
            Address = "0x123456789"
        });

        userAssetInfo1.ShowList.Count.ShouldBe(10);
    }


    [Fact]
    public async Task GetUserAssetTest()
    {
        var userTokenDto = new UserTokenDto
        {
            ChainId = "tDVV",
            Address = "0x123456789",
            Symbol = "USDT",
            Balance = 100
        };
        var userTokenDto1 = new UserTokenDto
        {
            ChainId = "tDVV",
            Address = "0x123456789",
            Symbol = "BTC",
            Balance = 300
        };
        var userTokenDto2 = new UserTokenDto
        {
            ChainId = "tDVV",
            Address = "0x123456789",
            Symbol = "EOS",
            Balance = 500
        };

        _graphQlProvider.AddUserToken(userTokenDto);
        var userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "tDVV",
            Address = "0x123456789"
        });
        userAssetInfo.ShowList.Count.ShouldBe(8);
        userAssetInfo.ShowList.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.ShowList.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.ShowList.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.ShowList.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.ShowList.First().Amount.ShouldBe("0.0001");
        userAssetInfo.HiddenList.Count.ShouldBe(0);

        _graphQlProvider.AddUserToken(userTokenDto1);
        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "tDVV",
            Address = "0x123456789"
        });
        userAssetInfo.ShowList.Count.ShouldBe(9);
        userAssetInfo.ShowList.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.ShowList.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.ShowList.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.ShowList.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.ShowList.First().Amount.ShouldBe("0.0001");
        userAssetInfo.ShowList.Last().ChainId.ShouldBe(userTokenDto1.ChainId);
        userAssetInfo.ShowList[1].Address.ShouldBe(userTokenDto1.Address);
        userAssetInfo.ShowList[1].Symbol.ShouldBe(userTokenDto1.Symbol);
        userAssetInfo.ShowList[1].Balance.ShouldBe(userTokenDto1.Balance);
        userAssetInfo.ShowList[1].Amount.ShouldBe("0.000003");
        userAssetInfo.HiddenList.Count.ShouldBe(0);

        _graphQlProvider.AddUserToken(userTokenDto2);
        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "tDVV",
            Address = "0x123456789"
        });
        userAssetInfo.ShowList.Count.ShouldBe(10);
        //userAssetInfo.HiddenList.Count().ShouldBe(1);
        userAssetInfo.ShowList.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.ShowList.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.ShowList.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.ShowList.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.ShowList.First().Amount.ShouldBe("0.0001");
        userAssetInfo.ShowList[2].ChainId.ShouldBe(userTokenDto2.ChainId);
        userAssetInfo.ShowList[2].Address.ShouldBe(userTokenDto2.Address);
        userAssetInfo.ShowList[2].Symbol.ShouldBe(userTokenDto1.Symbol);
        userAssetInfo.ShowList[2].Balance.ShouldBe(userTokenDto1.Balance);
        userAssetInfo.ShowList[2].Amount.ShouldBe("0.000003");


        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "eos",
            Address = "0x1234567890"
        });
        userAssetInfo.ShowList.Count.ShouldBe(10);
        userAssetInfo.HiddenList.Count.ShouldBe(0);
    }

    [Fact]
    public async Task SetDefaultToken_Success_Test()
    {
        var dto = new SetDefaultTokenDto
        {
            Address = "SPcgFjH76yLNV5M1cThyEsi2xzRH2LAJgGSzfs6redtFQTsot",
            TokenSymbol = "BTC"
        };
        var result = await _assetAppService.SetDefaultTokenAsync(dto);
        result.ShouldNotBeNull();

        var defaultToken =
            await _assetAppService.GetDefaultTokenAsync(new GetDefaultTokenDto { Address = dto.Address });
        defaultToken.ShouldNotBeNull();
        defaultToken.TokenSymbol.ShouldBe(dto.TokenSymbol);


        dto.TokenSymbol = "BTC1";
        var result2 = await _assetAppService.SetDefaultTokenAsync(dto);
        result2.Success.ShouldBe(false);
    }
}