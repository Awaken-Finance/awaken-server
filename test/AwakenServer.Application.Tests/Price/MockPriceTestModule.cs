using System.Collections.Generic;
using AwakenServer.Applications.GameOfTrust;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AwakenServer.Price
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule)
    )]
    public class MockPriceTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
            context.Services.AddSingleton<IPriceAppService, MockPriceAppService>();
            context.Services.Configure<StableCoinOptions>(o =>
            {
                o.Coins = new Dictionary<string, List<Coin>>();
                o.Coins["Ethereum"] = new List<Coin>
                {
                    new Coin{Address = "0xUSDT",Symbol = "USDT"},
                    new Coin{Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365",Symbol = "USDC"},
                    new Coin{Address = "0x06a6FaC8c710e53c4B2c2F96477119dA365",Symbol = "DAI"}
                };
                o.Coins["BSC"] = new List<Coin> {
                    new Coin{Address = "0xToken06a6FaC8c710e53c4B2c2F96477119dA362",Symbol = "BUSD"},
                };
            });
        }
    }
}