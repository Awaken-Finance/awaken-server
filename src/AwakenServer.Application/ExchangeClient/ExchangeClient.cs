using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Nethereum.Util;
using Newtonsoft.Json;

namespace AwakenServer.ExchangeClient
{
    public interface IExchangeClient
    {
        public string GetSymbol(string baseCurrency, string quoteCurrency);
        public Task<BigDecimal> GetPriceAsync(string symbol);
    }
    public abstract class ExchangeClient : IExchangeClient
    {
        protected async Task<T> MakeHttpGetRequest<T>(string url, Dictionary<string, string> headers = null)
        {
            // var proxy = new WebProxy
            // {
            //     Address = new Uri("http://127.0.0.1:1087"),
            // };
            // var clientHandler = new HttpClientHandler()
            // {
            //     Proxy = proxy,
            // };
            // using (var client = new HttpClient(clientHandler))
            using (var client = new HttpClient())
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.GetAsync(url);
                var statusCode = response.StatusCode;
                if (statusCode == HttpStatusCode.OK)
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    var jsonSerializerSettings = new JsonSerializerSettings();
                    jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
                    return JsonConvert.DeserializeObject<T>(respContent, jsonSerializerSettings);
                }
                else
                {
                    var respContent = await response.Content.ReadAsStringAsync();
                    throw new Exception(respContent);
                }
            }
        }

        public abstract string GetSymbol(string baseCurrency, string quoteCurrency);
        public abstract Task<BigDecimal> GetPriceAsync(string symbol);
    }
}