using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Price;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Farms.Services.Impl
{
    public class AElfFarmAppPriceService: IFarmAppPriceService, ITransientDependency // todo add AElf price service
    {
        private readonly ITokenPriceProvider _tokenPriceProvider;
        private readonly ITokenAppService _tokenAppService;
        private readonly IFarmTokenProvider _farmTokenProvider;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly IChainAppService _chainAppService;

        public AElfFarmAppPriceService(ITokenPriceProvider tokenPriceProvider, ITokenAppService tokenAppService,
            IFarmTokenProvider farmTokenProvider, ITradePairAppService tradePairAppService,
            IChainAppService chainAppService)
        {
            _tokenPriceProvider = tokenPriceProvider;
            _tokenAppService = tokenAppService;
            _farmTokenProvider = farmTokenProvider;
            _tradePairAppService = tradePairAppService;
            _chainAppService = chainAppService;
        }

        public async Task<List<FarmPriceDto>> GetSwapTokenPricesAsync(GetSwapTokenPricesInput input)
        {
            var chain = await _chainAppService.GetChainAsync(input.ChainId);
            var farmTokens = new Dictionary<string,FarmToken>();
            foreach (var symbol in input.TokenSymbol)
            {
                var farmToken = _farmTokenProvider.GetFarmToken(chain.Name, symbol);
                farmTokens[farmToken.TradePairAddress] = farmToken;
            }
            var pairs = await _tradePairAppService.GetListAsync(input.ChainId, farmTokens.Keys);
            var pairDic = pairs.ToDictionary(p => p.Address, p => p);

            var result = new List<FarmPriceDto>();
            foreach (var token in farmTokens)
            {
                var price = 0d;
                if (pairDic.TryGetValue(token.Key, out var pair))
                {
                    price = (pair.TVL / double.Parse(pair.TotalSupply));
                }
                
                result.Add(new FarmPriceDto
                {
                    ChainId = input.ChainId,
                    TokenAddress = token.Value.Address,
                    TokenSymbol = token.Value.Symbol,
                    Price = price.ToString()
                });

            }

            return result;
        }

        public async Task<string> GetTokenPriceAsync(GetTokenPriceInput input)
        {
            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = input.ChainId,
                Symbol = input.Symbol
            });

            if (token == null)
            {
                return "0";
            }

            return (await _tokenPriceProvider.GetTokenUSDPriceAsync(input.ChainId, token.Symbol)).ToString();
        }
    }
}