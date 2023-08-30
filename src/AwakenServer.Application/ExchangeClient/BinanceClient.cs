using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Util;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.ExchangeClient
{
    public interface IBinanceClient : IExchangeClient
    {
        
    }
    
    public class BinanceClient : ExchangeClient, IBinanceClient, ISingletonDependency
    {
        public string BaseUrl { get; } = "https://api.binance.com";

        public override string GetSymbol(string baseCurrency, string quoteCurrency)
        {
            return ($"{baseCurrency}{quoteCurrency}").ToUpper();
        }

        public override async Task<BigDecimal> GetPriceAsync(string symbol)
        {
            try
            {
                var result = await MakeHttpGetRequest<JObject>($"{BaseUrl}/api/v3/ticker/price?symbol={symbol}",
                    new Dictionary<string, string>());
                return BigDecimal.Parse(result.Value<string>("price"));
            }
            catch
            {
                return 0;
            }
        }
    }
}