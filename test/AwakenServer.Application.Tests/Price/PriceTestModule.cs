using System.Collections.Generic;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AwakenServer.Price
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule)
    )]
    public class PriceTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
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

            Configure<ISTARTokenOptions>(options =>
            {
                options.Address = "0xISTAR";
                options.Decimals = 18;
                options.Symbol = "ISTAR";
                options.InitPrice = "0.05";
            });

            Configure<FarmTokenOptions>(options =>
            {
                var ethOption = new TokenOption
                {
                    Address = "0xETH",
                    Decimals = 18,
                    Symbol = "ETH"
                };
                var btcOption = new TokenOption
                {
                    Address = "0xBTC",
                    Decimals = 18,
                    Symbol = "BTC"
                };
                var chainName = "Ethereum";
                options.FarmTokens = new Dictionary<string, FarmToken>
                {
                    {
                        $"{chainName}-0xGEther", new FarmToken
                        {
                            Address = "0xGEther",
                            Tokens = new[] {ethOption},
                            Type = FarmTokenType.GToken,
                            ChainName = chainName
                        }
                    },
                    {
                        $"{chainName}-0xAbtc", new FarmToken
                        {
                            Address = "0xAbtc",
                            Tokens = new[] {btcOption},
                            Type = FarmTokenType.AToken,
                            ChainName = chainName,
                            LendingPool = "0xLendingPool"
                        }
                    },
                    {
                        $"{chainName}-0xLpToken", new FarmToken
                        {
                            Address = "0xLpToken",
                            Tokens = new[] {ethOption, btcOption},
                            Type = FarmTokenType.LpToken,
                            ChainName = chainName
                        }
                    },
                    {
                        $"{chainName}-0xOtherLPToken", new FarmToken
                        {
                            Address = "0xOtherLPToken",
                            Tokens = new[] {ethOption, btcOption},
                            Type = FarmTokenType.OtherLpToken,
                            ChainName = chainName
                        }
                    }
                };
            });
        }
    }
}