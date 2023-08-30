using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using AwakenServer.Web3;
using Nethereum.Contracts;
using Nethereum.Util;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Price
{
    public class MockWeb3Provider : IWeb3Provider, IBlockchainClientProvider
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
            return Task.FromResult((BigDecimal)50);
        }

        public Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol = null)
        {
            return Task.FromResult(new TokenDto
            {
                Address = address,
                Decimals = 8,
                Symbol = chainName
            });
        }

        public Task<BigDecimal> GetGTokenExchangeRateAsync(string chainName, string address)
        {
            return Task.FromResult((BigDecimal)2);
        }

        public Task<BigDecimal> GetATokenExchangeRateAsync(string chainName, string address, string lendingPool)
        {
            return Task.FromResult((BigDecimal)3);
        }

        public Task<T> QueryAsync<TFunctionMessage, T>(string chainName, string address) where TFunctionMessage : FunctionMessage, new()
        {
            throw new System.NotImplementedException();
        }
    }
}