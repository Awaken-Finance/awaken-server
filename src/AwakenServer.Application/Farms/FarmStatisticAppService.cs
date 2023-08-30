using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Farms.Options;
using AwakenServer.Farms.Services;
using AwakenServer.Price.Dtos;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Volo.Abp;

namespace AwakenServer.Farms
{
    [RemoteService(IsEnabled = false)]
    public class FarmStatisticAppService : AwakenServerAppService, IFarmStatisticAppService
    {
        private readonly IFarmAppService _farmAppService;
        private readonly IChainAppService _chainAppService;
        private readonly IFarmAppPriceService _farmAppPriceService;
        private readonly string _mainToken;
        private readonly string _btcToken;

        public FarmStatisticAppService(IFarmAppService farmAppService,
            IChainAppService chainAppService,
            IFarmAppPriceService farmAppPriceService,
            IOptionsSnapshot<FarmOption> farmOptionsSnapshot)
        {
            _farmAppService = farmAppService;
            _chainAppService = chainAppService;
            _farmAppPriceService = farmAppPriceService;
            _mainToken = farmOptionsSnapshot.Value.MainToken;
            _btcToken = farmOptionsSnapshot.Value.BtcSymbol;
        }

        public async Task<TotalPoolStatistics> GetPoolsStatisticInfo(GetPoolsTotalStatisticInput input)
        {
            if (!string.IsNullOrEmpty(input.ChainId) || input.FarmId.HasValue || input.PoolId.HasValue)
            {
                return await GetChainPoolsStatisticInfo(input);
            }

            var chains = (await _chainAppService.GetListAsync(new GetChainInput())).Items;
            var totalDepositValue = 0m;
            var totalRevenueValue = 0m;
            foreach (var chain in chains)
            {
                var totalPoolStatistics = await GetChainPoolsStatisticInfo(new GetPoolsTotalStatisticInput
                {
                    ChainId = chain.Id
                });
                totalDepositValue += totalPoolStatistics.TotalDepositValue;
                totalRevenueValue += totalPoolStatistics.TotalRevenueValue;
            }

            return new TotalPoolStatistics
            {
                TotalDepositValue = totalDepositValue,
                TotalRevenueValue = totalRevenueValue
            };
        }

        public async Task<TotalUserStatistics> GetUsersStatisticInfo(GetUsersTotalStatisticInput input)
        {
            var userInfos = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                User = input.User,
                ChainId = input.ChainId,
                FarmId = input.FarmId,
                PoolId = input.PoolId
            });

            var totalDepositUsdtValue = 0m;
            var totalUsdt = BigInteger.Zero;
            var totalProjectToken = BigInteger.Zero;
            foreach (var userInfo in userInfos.Items)
            {
                totalDepositUsdtValue += await GetSwapTokenUsdtValueAsync(userInfo.ChainId, userInfo.SwapToken.Address,
                    userInfo.SwapToken.Symbol,
                    userInfo.CurrentDepositAmount, userInfo.SwapToken.Decimals);
                totalProjectToken += BigInteger.Parse(userInfo.AccumulativeDividendProjectTokenAmount);
                totalUsdt += BigInteger.Parse(userInfo.AccumulativeDividendUsdtAmount);
            }

            var chainId = input.ChainId;
            var totalRevenueValue = CalculateTokenValue(1m, totalUsdt, FarmConstant.UsdtDecimal) +
                                    CalculateTokenValue(await GetProjectTokenUsdtPriceAsync(chainId), totalProjectToken,
                                        FarmConstant.UsdtDecimal);
            var totalDepositBtcValue = await GetBtcValueOfUsdtAsync(chainId, totalDepositUsdtValue);
            return new TotalUserStatistics
            {
                TotalDepositBtcValue = totalDepositBtcValue,
                TotalDepositUsdtValue = totalDepositUsdtValue,
                TotalRevenueUsdtValue = totalRevenueValue
            };
        }

        private async Task<TotalPoolStatistics> GetChainPoolsStatisticInfo(GetPoolsTotalStatisticInput input)
        {
            var pools = await _farmAppService.GetFarmPoolListAsync(new GetFarmPoolInput
            {
                ChainId = input.ChainId,
                FarmId = input.FarmId,
                PoolId = input.PoolId,
                IsUpdateReward = true
            });

            var totalDepositValue = 0m;
            var totalProjectToken = BigInteger.Zero;
            var totalUsdt = BigInteger.Zero;
            foreach (var pool in pools.Items)
            {
                totalDepositValue += CalculateTokenValue(pool.SwapToken.TokenPrice, pool.TotalDepositAmount,
                    pool.SwapToken.Decimals);
                totalUsdt += BigInteger.Parse(pool.AccumulativeDividendUsdt);
                totalUsdt += BigInteger.Parse(pool.PendingUsdt);
                totalProjectToken += BigInteger.Parse(pool.AccumulativeDividendProjectToken);
                totalProjectToken += BigInteger.Parse(pool.PendingProjectToken);
            }

            var totalUsdtValue = (decimal) ((BigDecimal) totalUsdt / BigInteger.Pow(10, FarmConstant.UsdtDecimal));
            var totalProjectTokenValue = (decimal) ((BigDecimal) totalProjectToken * await GetProjectTokenUsdtPriceAsync(input.ChainId) /
                                           BigInteger.Pow(10, FarmConstant.ProjectTokenDecimal));
            return new TotalPoolStatistics
            {
                TotalDepositValue = totalDepositValue,
                TotalRevenueValue = totalUsdtValue + totalProjectTokenValue
            };
        }

        private async Task<decimal> GetSwapTokenUsdtValueAsync(string chainId, string tokenAddress, string tokenSymbol,
            string tokenAmount,
            int tokenDecimal)
        {
            var farmTokenPriceDto = (await _farmAppPriceService.GetSwapTokenPricesAsync(new GetSwapTokenPricesInput
            {
                ChainId = chainId,
                TokenAddresses = new[] {tokenAddress},
                TokenSymbol = new []{tokenSymbol}
            })).First();
            var tokenPrice = decimal.Parse(farmTokenPriceDto.Price);
            return
                CalculateTokenValue(tokenPrice, tokenAmount, tokenDecimal);
        }

        private decimal CalculateTokenValue(decimal tokenPrice, string tokenAmount, int tokenDecimal)
        {
            return
                (decimal) (BigDecimal.Parse(tokenAmount) * tokenPrice / BigInteger.Pow(10, tokenDecimal));
        }

        private decimal CalculateTokenValue(decimal tokenPrice, BigInteger tokenAmount, int tokenDecimal)
        {
            return
                (decimal) ((BigDecimal) tokenAmount * tokenPrice / BigInteger.Pow(10, tokenDecimal));
        }

        private async Task<decimal> GetProjectTokenUsdtPriceAsync(string chainId)
        {
            var tokenPrice = await _farmAppPriceService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                ChainId = chainId,
                Symbol = _mainToken
            });
            return decimal.Parse(tokenPrice);
        }

        private async Task<decimal> GetBtcValueOfUsdtAsync(string chainId, decimal usdtAmount)
        {
            var btcPrice = await _farmAppPriceService.GetTokenPriceAsync(new GetTokenPriceInput
            {
                ChainId = chainId,
                Symbol = _btcToken
            });
            return usdtAmount / decimal.Parse(btcPrice);
        }
    }
}