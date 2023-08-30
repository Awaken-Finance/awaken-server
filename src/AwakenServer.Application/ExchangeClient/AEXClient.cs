using System.Collections.Generic;
using System.Threading.Tasks;
using Nethereum.Util;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.ExchangeClient
{
    public interface IAEXClient : IExchangeClient
    {
        
    }
    
    public class AEXClient : ExchangeClient,IAEXClient, ISingletonDependency
    {
        public string BaseUrl { get; } = "https://api.aex.zone/v3/";

        public override string GetSymbol(string baseCurrency, string quoteCurrency)
        {
            return ($"{baseCurrency}_{quoteCurrency}").ToUpper();
        }
        
        public override async Task<BigDecimal> GetPriceAsync(string symbol)
        {
            try
            {
                var tokens = symbol.Split("_");
                var result = await MakeHttpGetRequest<JObject>(
                    $"{BaseUrl}/ticker.php?coinname={tokens[0]}&&mk_type={tokens[1]}",
                    new Dictionary<string, string>());

                return BigDecimal.Parse(result["data"]["ticker"]["last"].ToString());
            }
            catch
            {
                return 0;
            }
        }
    }
}