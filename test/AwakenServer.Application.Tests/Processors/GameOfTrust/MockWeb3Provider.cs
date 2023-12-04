using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using AwakenServer.Web3;
using Nethereum.Contracts;
using Nethereum.Util;

namespace AwakenServer.Applications.GameOfTrust
{
    public class MockWeb3Provider : IWeb3Provider,IBlockchainClientProvider
    {
        public string ChainType { get; } = "Ethereum";

        public Nethereum.Web3.Web3 GetWeb3(string chainName)
        {
            throw new System.NotImplementedException();
        }
        public Task<long> GetBlockNumberAsync(string chainName)
        {
            return Task.FromResult(1000L);
        }

        public Task<BigDecimal> GetTokenTotalSupplyAsync(string chainName, string address)
        {
            throw new System.NotImplementedException();
        }

        public Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol = null)
        {
            throw new System.NotImplementedException();
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
            throw new System.NotImplementedException();
        }
        public async Task<GetBalanceOutput> GetBalanceAsync(string chainName, string address, string contractAddress,
            string symbol)
        {
            throw new System.NotImplementedException();
        }
    }
}