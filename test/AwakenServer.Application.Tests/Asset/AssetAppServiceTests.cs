using System;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Trade;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AwakenServer.Asset;

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

        userAssetInfo.Iterms.Count.ShouldBe(2);

        var userAssetInfo1 = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "CAElf",
            Address = "0x123456789"
        });

        userAssetInfo1.Iterms.Count.ShouldBe(2);
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
        userAssetInfo.Iterms.Count.ShouldBe(1);
        userAssetInfo.Iterms.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.Iterms.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.Iterms.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.Iterms.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.Iterms.First().Amount.ShouldBe("0.0001");
        // userAssetInfo.HiddenList.Count.ShouldBe(0);

        _graphQlProvider.AddUserToken(userTokenDto1);
        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789"
        });
        userAssetInfo.Iterms.Count.ShouldBe(2);
        userAssetInfo.Iterms.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.Iterms.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.Iterms.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.Iterms.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.Iterms.First().Amount.ShouldBe("0.0001");
        userAssetInfo.Iterms.Last().ChainId.ShouldBe(userTokenDto1.ChainId);
        userAssetInfo.Iterms.Last().Address.ShouldBe(userTokenDto1.Address);
        userAssetInfo.Iterms.Last().Symbol.ShouldBe(userTokenDto1.Symbol);
        userAssetInfo.Iterms.Last().Balance.ShouldBe(userTokenDto1.Balance);
        userAssetInfo.Iterms.Last().Amount.ShouldBe("0.000003");
        // userAssetInfo.HiddenList.Count.ShouldBe(0);

        _graphQlProvider.AddUserToken(userTokenDto2);
        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "Ethereum",
            Address = "0x123456789"
        });
        userAssetInfo.Iterms.Count.ShouldBe(3);
        //userAssetInfo.HiddenList.Count().ShouldBe(1);
        userAssetInfo.Iterms.First().ChainId.ShouldBe(userTokenDto.ChainId);
        userAssetInfo.Iterms.First().Address.ShouldBe(userTokenDto.Address);
        userAssetInfo.Iterms.First().Symbol.ShouldBe(userTokenDto.Symbol);
        userAssetInfo.Iterms.First().Balance.ShouldBe(userTokenDto.Balance);
        userAssetInfo.Iterms.First().Amount.ShouldBe("0.0001");
        userAssetInfo.Iterms.Last().ChainId.ShouldBe(userTokenDto2.ChainId);
        userAssetInfo.Iterms.Last().Address.ShouldBe(userTokenDto2.Address);
        userAssetInfo.Iterms.Last().Symbol.ShouldBe(userTokenDto1.Symbol);
        userAssetInfo.Iterms.Last().Balance.ShouldBe(userTokenDto1.Balance);
        userAssetInfo.Iterms.Last().Amount.ShouldBe("0.000003");


        userAssetInfo = await _assetAppService.GetUserAssetInfoAsync(new GetUserAssetInfoDto
        {
            ChainId = "eos",
            Address = "0x1234567890"
        });
        userAssetInfo.Iterms.Count.ShouldBe(2);
        // userAssetInfo.HiddenList.Count.ShouldBe(0);
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