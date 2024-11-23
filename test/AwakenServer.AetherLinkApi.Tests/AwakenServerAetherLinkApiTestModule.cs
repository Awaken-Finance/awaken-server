using System.Collections.Generic;
using Aetherlink.PriceServer;
using Aetherlink.PriceServer.Dtos;
using Awaken.Common.HttpClient;
using AwakenServer.AetherLinkApi;
using AwakenServer.Commons;
using AwakenServer.Grains.Grain.Tokens.TokenPrice;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Newtonsoft.Json;
using Volo.Abp.Modularity;

namespace AwakenServer;

[DependsOn(
    typeof(AwakenServerAetherLinkApiModule),
    typeof(AwakenServerTestBaseModule)
)]
public class AwakenServerAetherLinkApiTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var mockPriceServiceProvider = new Mock<IPriceServerProvider>();
        mockPriceServiceProvider.Setup(provider => provider.GetAggregatedTokenPriceAsync(
            It.IsAny<GetAggregatedTokenPriceRequestDto>()
        )).ReturnsAsync(new AggregatedPriceResponseDto { Data = new PriceDto()
        {
            Price = 1000000,
            Decimal = 6
        }});
        mockPriceServiceProvider.Setup(provider => provider.GetDailyPriceAsync(
            It.IsAny<GetDailyPriceRequestDto>()
        )).ReturnsAsync(new DailyPriceResponseDto { Data = new PriceDto()
        {
            Price = 200000000
        }});
        
        context.Services.AddSingleton<IPriceServerProvider>(mockPriceServiceProvider.Object);
        context.Services.AddSingleton<AetherLinkTokenPriceProvider>();
    }
}