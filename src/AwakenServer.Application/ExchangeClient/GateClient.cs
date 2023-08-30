using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Util;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.ExchangeClient
{
    public interface IGateClient : IExchangeClient
    {
        
    }
    
    public class GateClient : ExchangeClient, IGateClient, ISingletonDependency
    {
        public string BaseUrl { get; } = "https://api.gateio.ws";

        public override string GetSymbol(string baseCurrency, string quoteCurrency)
        {
            return ($"{baseCurrency}_{quoteCurrency}").ToUpper();
        }

        public override async Task<BigDecimal> GetPriceAsync(string symbol)
        {
            try
            {
                var result = await MakeHttpGetRequest<JArray>($"{BaseUrl}/api/v4/spot/tickers?currency_pair={symbol}",
                    new Dictionary<string, string>());
                return BigDecimal.Parse(result[0].Value<string>("last"));
            }
            catch
            {
                return 0;
            }
        }
    }
}