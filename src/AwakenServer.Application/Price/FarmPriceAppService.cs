using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Price.Dtos;
using AwakenServer.Trade;
using AwakenServer.Web3;
using Nethereum.Util;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AwakenServer.Price
{
    [RemoteService(IsEnabled = false)]
    public class FarmPriceAppService : ApplicationService, IFarmPriceAppService
    {
        private readonly ITradePairAppService _tradePairAppService;
        private readonly ILendingTokenPriceAppService _lendingTokenPriceAppService;
        private readonly IOtherLpTokenAppService _otherLpTokenAppService;
        private readonly IChainAppService _chainAppService;
        private readonly IWeb3Provider _web3Provider;
        private readonly IFarmTokenProvider _farmTokenProvider;

        public FarmPriceAppService(ITradePairAppService tradePairAppService, 
            IOtherLpTokenAppService otherLpTokenAppService, IWeb3Provider web3Provider, IChainAppService chainAppService, 
            ILendingTokenPriceAppService lendingTokenPriceAppService, IFarmTokenProvider farmTokenProvider)
        {
            _tradePairAppService = tradePairAppService;
            _otherLpTokenAppService = otherLpTokenAppService;
            _web3Provider = web3Provider;
            _chainAppService = chainAppService;
            _lendingTokenPriceAppService = lendingTokenPriceAppService;
            _farmTokenProvider = farmTokenProvider;
        }

        public async Task<List<FarmPriceDto>> GetPricesAsync(GetFarmTokenPriceInput input)
        {
            var chain = await _chainAppService.GetChainAsync(input.ChainId);
            var farmTokens = input.TokenAddresses.Distinct().Select(t => _farmTokenProvider.GetFarmToken(chain.Name, t))
                .ToList();

            var gTokenPrices = await GetLendingTokenPrices(input.ChainId,
                farmTokens.Where(f => f.Type == FarmTokenType.GToken && f.ChainName == chain.Name).ToList(),
                GetGTokenExchangeRateAsync);
            var aTokenPrices = await GetLendingTokenPrices(input.ChainId,
                farmTokens.Where(f => f.Type == FarmTokenType.AToken && f.ChainName == chain.Name).ToList(),
                _web3Provider.GetATokenExchangeRateAsync);
            var otherLpTokenPrices = await GetOtherLpTokenPricesAsync(input.ChainId,
                farmTokens.Where(f => f.Type == FarmTokenType.OtherLpToken && f.ChainName == chain.Name)
                    .Select(f => f.Address).ToArray());
            var lpTokenPrices = await GetLpTokenPricesAsync(input.ChainId,
                farmTokens.Where(f => f.Type == FarmTokenType.LpToken && f.ChainName == chain.Name)
                    .Select(f => f.Address).ToList());
            return gTokenPrices.Union(aTokenPrices).Union(otherLpTokenPrices).Union(lpTokenPrices).ToList();
        }

        private async Task<List<FarmPriceDto>> GetLendingTokenPrices(string chainId, List<FarmToken> farmTokens, Func<string, string, string, Task<BigDecimal>> getExchangeRateFunc)
        {
            var tokenPrices = await _lendingTokenPriceAppService.GetPricesAsync(chainId, farmTokens.Select(f=>f.Tokens[0].Address).ToArray());
            var tokenPricesDic = tokenPrices.ToDictionary(price => price.Token.Address, price => price.Price);
            var dtos = new List<FarmPriceDto>();
            foreach (var farmToken in farmTokens)
            {
                var exchangeRate = await getExchangeRateFunc(farmToken.ChainName,
                    farmToken.Type == FarmTokenType.GToken ? farmToken.Address : farmToken.Tokens[0].Address,
                    farmToken.LendingPool);
                if (farmToken.Type == FarmTokenType.GToken)
                {
                    exchangeRate = exchangeRate / BigInteger.Pow(10, farmToken.Tokens[0].Decimals - 8);
                }
                dtos.Add(new FarmPriceDto
                {
                    ChainId = chainId,
                    TokenAddress = farmToken.Address,
                    Price = (exchangeRate * (tokenPricesDic.TryGetValue(farmToken.Tokens[0].Address, out var price) ? BigDecimal.Parse(price):0)).ToString()
                });
            }

            return dtos;
        }
        
        // public async Task<List<FarmPriceDto>> GetATokenPrices(Guid chainId, string[] tokenAddresses)
        // {
        //     var chain = await _chainAppService.GetChainAsync(chainId);
        //     var underlyingTokenAddressList = tokenAddresses.Select(t => GetToken(chain.Name, t)).ToArray();
        //     var tokenPrices = await _lendingTokenPriceAppService.GetPricesAsync(chain.Id, underlyingTokenAddressList);
        //     var tokenPricesDic = tokenPrices.ToDictionary(price => price.Token.Address, price => price.Price);
        //     var dtos = new List<FarmPriceDto>();
        //     foreach (var tokenAddress in tokenAddresses)
        //     {
        //         dtos.Add(new FarmPriceDto
        //         {
        //             ChainId = chainId,
        //             TokenAddress = tokenAddress,
        //             Price = (await GetATokenExchangeRateAsync(chain.Name, tokenAddress) * BigDecimal.Parse(tokenPricesDic[GetToken(chain.Name, tokenAddress)])).ToString()
        //         });
        //     }
        //
        //     return dtos;
        // }
        
        private async Task<BigDecimal> GetGTokenExchangeRateAsync(string chainName, string address, string lendingPool)
        {
            return await _web3Provider.GetGTokenExchangeRateAsync(chainName, address);
        }
        
        private async Task<List<FarmPriceDto>> GetOtherLpTokenPricesAsync(string chainId, IEnumerable<string> tokenAddresses)
        {
            var chain = await _chainAppService.GetChainAsync(chainId);
            var indexDtoList = await _otherLpTokenAppService.GetOtherLpTokenIndexListAsync(chainId, tokenAddresses);
            var tokenPrices = await _lendingTokenPriceAppService.GetPricesAsync(new GetPricesInput
            {
                ChainId = chainId,
                TokenIds = indexDtoList.Select(i => i.Token0.Id).Union(indexDtoList.Select(i => i.Token1.Id)).Distinct()
                    .ToArray()
            });
            var tokenPricesDic = tokenPrices.ToDictionary(price => price.Token.Id, price => price.Price);
            var dtoList = new List<FarmPriceDto>();
            foreach (var otherLpToken in indexDtoList)
            {
                var totalSupply = await _web3Provider.GetTokenTotalSupplyAsync(chain.Name, otherLpToken.Address);
                var reserve0Value = BigDecimal.Parse(otherLpToken.Reserve0) *
                                    (tokenPricesDic.TryGetValue(otherLpToken.Token0.Id, out var price0)
                                        ? BigDecimal.Parse(price0)
                                        : 0);
                var reserve1Value = BigDecimal.Parse(otherLpToken.Reserve1) *
                                    (tokenPricesDic.TryGetValue(otherLpToken.Token1.Id, out var price1)
                                        ? BigDecimal.Parse(price1)
                                        : 0);
                dtoList.Add(new FarmPriceDto
                {
                    ChainId = chainId,
                    TokenAddress = otherLpToken.Address,
                    Price = ((reserve0Value + reserve1Value) / totalSupply).ToString()
                });
            }

            return dtoList;
        }

        private async Task<List<FarmPriceDto>> GetLpTokenPricesAsync(string chainId, IEnumerable<string> tokenAddresses)
        {
            var tradePairs = await _tradePairAppService.GetListAsync(chainId, tokenAddresses);
            var dtos = new List<FarmPriceDto>();
            foreach (var tradePair in tradePairs)
            {
                dtos.Add(new FarmPriceDto
                {
                    ChainId = chainId,
                    Price = (tradePair.TVL / double.Parse(tradePair.TotalSupply)).ToStringInvariant(),
                    TokenAddress = tradePair.Address
                });
            }

            return dtos;
        }
    }
}