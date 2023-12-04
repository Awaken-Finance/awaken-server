using System;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using AwakenServer.Web3.FunctionMessages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using Nethereum.Contracts;
using Nethereum.StandardTokenEIP20.ContractDefinition;
using Nethereum.Util;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Web3
{
    public interface IWeb3Provider
    {
        Task<long> GetBlockNumberAsync(string chainName);

        Task<BigDecimal> GetTokenTotalSupplyAsync(string chainName, string address);

        Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol = null);

        Task<BigDecimal> GetGTokenExchangeRateAsync(string chainName, string address);

        Task<BigDecimal> GetATokenExchangeRateAsync(string chainName, string address, string lendingPool);

        Task<T> QueryAsync<TFunctionMessage, T>(string chainName, string address)
            where TFunctionMessage : FunctionMessage, new();
    }
    
    
    public class Web3Provider : IWeb3Provider,IBlockchainClientProvider, ITransientDependency
    {
        public string ChainType { get; } = "Ethereum";
        
        private const int CacheExpireMinutes = 1;
        
        private readonly ApiOptions _apiOptions;
        
        private readonly IDistributedCache<string> _contractReturnValueCache;
        private readonly IBlockchainClientFactory<Nethereum.Web3.Web3> _blockchainClientFactory;

        public Web3Provider(IOptionsSnapshot<ApiOptions> apiOptions,
            IDistributedCache<string> contractReturnValueCache,
            IBlockchainClientFactory<Nethereum.Web3.Web3> blockchainClientFactory)
        {
            _contractReturnValueCache = contractReturnValueCache;
            _blockchainClientFactory = blockchainClientFactory;
            _apiOptions = apiOptions.Value;
        }

        public async Task<long> GetBlockNumberAsync(string chainName)
        {
            var web3 = _blockchainClientFactory.GetClient(chainName);
            var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return latestBlockNumber.ToLong();
        }
        
        public async Task<BigDecimal> GetATokenExchangeRateAsync(string chainName, string address, string lendingPool)
        {
            var key = $"ATokenExchangeRate-{chainName}-{address}-{lendingPool}";
            var exchangeRate = await _contractReturnValueCache.GetAsync(key);
            if (!string.IsNullOrWhiteSpace(exchangeRate))
            {
                return BigDecimal.Parse(exchangeRate);
            }
            var web3 = _blockchainClientFactory.GetClient(chainName);
            var contractHandler = web3.Eth.GetContractHandler(lendingPool);
            var result = await contractHandler.QueryAsync<GetReserveNormalizedIncomeFunction, BigInteger>(new GetReserveNormalizedIncomeFunction
            {
                Asset = address
            });
            
            var value = ((BigDecimal) result / BigInteger.Pow(10, 27));
            
            await _contractReturnValueCache.SetAsync(key, value.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(CacheExpireMinutes)
            });
            
            return value;
        }
        
        public async Task<BigDecimal> GetGTokenExchangeRateAsync(string chainName, string address)
        {
            var key = $"GTokenExchangeRate-{chainName}-{address}";
            var exchangeRate = await _contractReturnValueCache.GetAsync(key);
            if (!string.IsNullOrWhiteSpace(exchangeRate))
            {
                return BigDecimal.Parse(exchangeRate);
            }
            var web3 = _blockchainClientFactory.GetClient(chainName);
            var contractHandler = web3.Eth.GetContractHandler(address);
            var result = await contractHandler.QueryAsync<ExchangeRateCurrentFunction, BigInteger>();
            
            var value = (BigDecimal) result / BigInteger.Pow(10, 18);
            
            await _contractReturnValueCache.SetAsync(key, value.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(CacheExpireMinutes)
            });
            
            return value;
        }
        
        public async Task<BigDecimal> GetTokenTotalSupplyAsync(string chainName, string address)
        {
            var key = $"TokenTotalSupply-{chainName}-{address}";
            var totalSupply = await _contractReturnValueCache.GetAsync(key);
            if (!string.IsNullOrWhiteSpace(totalSupply))
            {
                return BigDecimal.Parse(totalSupply);
            }
            var web3 = _blockchainClientFactory.GetClient(chainName);
            var contractHandler = web3.Eth.GetContractHandler(address);
            var result = await contractHandler.QueryAsync<TotalSupplyFunction, BigInteger>();
            var value = (BigDecimal) result /
                        BigInteger.Pow(10, await contractHandler.QueryAsync<DecimalsFunction, int>());
            
            await _contractReturnValueCache.SetAsync(key, value.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(CacheExpireMinutes)
            });
            
            return value;
        }

        public async Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbo)
        {
            var web3 = _blockchainClientFactory.GetClient(chainName);
            var contractHandler = web3.Eth.GetContractHandler(address);

            return new TokenDto
            {
                Address = address,
                Decimals = await contractHandler.QueryAsync<DecimalsFunction, int>(),
                Symbol = await contractHandler.QueryAsync<SymbolFunction, string>()
            };
        }

        public async Task<T> QueryAsync<TFunctionMessage, T>(string chainName, string address)
            where TFunctionMessage : FunctionMessage, new()
        {
            var web3 = _blockchainClientFactory.GetClient(chainName);
            var contractHandler = web3.Eth.GetContractHandler(address);
            return await contractHandler.QueryAsync<TFunctionMessage, T>();
        }

        public async Task<GetBalanceOutput> GetBalanceAsync(string chainName, string address, string contractAddress,
            string symbol)
        {
            throw new NotImplementedException();
        }

        
    }
}