using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Price.Processors;
using AwakenServer.Tokens;
using AwakenServer.Web3;
using Nethereum.Contracts;
using Nethereum.Util;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Price
{
    public class MockWeb3Provider : IWeb3Provider,IBlockchainClientProvider, ITransientDependency
    {
        public string ChainType { get; } = "Ethereum";

        public Nethereum.Web3.Web3 GetWeb3(string chainName)
        {
            return new Nethereum.Web3.Web3();
        }
        
        public Task<long> GetBlockNumberAsync(string chainName)
        {
            throw new System.NotImplementedException();
        }

        public Task<BigDecimal> GetTokenTotalSupplyAsync(string chainName, string address)
        {
            throw new System.NotImplementedException();
        }

        public Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol = null)
        {
            symbol = address == "0xETH" ? "ETH" : "BTC";
            return Task.FromResult(new TokenDto
            {
                Address = address,
                Decimals = 18,
                Symbol = symbol
            });
        }

        public Task<BigDecimal> GetGTokenExchangeRateAsync(string chainName, string address)
        {
            throw new System.NotImplementedException();
        }

        public Task<BigDecimal> GetATokenExchangeRateAsync(string chainName, string address, string lendingPool)
        {
            throw new System.NotImplementedException();
        }

        public Task<T> QueryAsync<TFunctionMessage, T>(string chainName, string address) where TFunctionMessage : FunctionMessage, new()
        {
            if (typeof(TFunctionMessage) == typeof(Token0Function)) return Task.FromResult((T)"0xBTC".Clone());
            if (typeof(TFunctionMessage) == typeof(Token1Function)) return Task.FromResult((T)"0xETH".Clone());
            return null;
        }
    }
}