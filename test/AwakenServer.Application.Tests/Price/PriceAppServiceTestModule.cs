using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Applications.GameOfTrust;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AwakenServer.Price
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule)
    )]
    public class PriceAppServiceTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ITokenPriceProvider, MockTokenPriceProvider>();
            context.Services.Configure<TokenPriceOptions>(o =>
            {
                o.PriceExpirationTimeSeconds = 2;
                o.UsdtPriceTokens = new List<string>()
                {
                    "SGR-1"
                };
                o.PriceTokenMapping = new Dictionary<string, string>()
                {
                    {"ELF", "elf-usd"},
                    {"USDT", "usdt-usd"},
                    {"ETH", "eth-usd"},
                    {"USDC", "usdc-usd"},
                    {"DAI", "dai-usd"},
                    {"BNB", "bnb-usd"},
                    {"BTC", "btc-usd"},
                    {"SGR-1", "sgr-usdt"},
                    {"TESTCACHE", "testcache-usdt"}
                };
            });
            
        }
    }
}