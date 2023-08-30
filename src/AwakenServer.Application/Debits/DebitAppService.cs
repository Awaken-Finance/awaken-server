using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Debits.DebitAppDto;
using AwakenServer.Debits.Entities.Es;
using AwakenServer.Debits.Helpers;
using AwakenServer.Debits.Options;
using AwakenServer.Debits.Services;
using AwakenServer.Price.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Nethereum.Util;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Debits
{
    [RemoteService(IsEnabled = false)]
    public class DebitAppService : AwakenServerAppService, IDebitAppService
    {
        private readonly INESTReaderRepository<CompController, Guid> _compControllerReaderRepository;
        private readonly INESTReaderRepository<CToken, Guid> _cTokenReaderRepository;
        private readonly INESTReaderRepository<CTokenUserInfo, Guid> _cTokenUserInfoReaderRepository;
        private readonly INESTReaderRepository<CTokenRecord, Guid> _cTokenRecordReaderRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ITokenRateCalculateService _tokenRateCalculateService;
        private readonly IDebitAppPriceService _debitAppPriceService;
        private readonly string _mainToken;
        private readonly ILogger<DebitAppService> _logger;

        public DebitAppService(INESTReaderRepository<CompController, Guid> compControllerReaderRepository,
            INESTReaderRepository<CToken, Guid> cTokenReaderRepository,
            INESTReaderRepository<CTokenUserInfo, Guid> cTokenUserInfoReaderRepository,
            INESTReaderRepository<CTokenRecord, Guid> cTokenRecordReaderRepository,
            ITokenRateCalculateService tokenRateCalculateService, IChainAppService chainAppService,
            IDebitAppPriceService debitAppPriceService, 
            IOptionsSnapshot<DebitOption> debitOptionsSnapshot,
            ILogger<DebitAppService> logger)
        {
            _compControllerReaderRepository = compControllerReaderRepository;
            _cTokenReaderRepository = cTokenReaderRepository;
            _cTokenUserInfoReaderRepository = cTokenUserInfoReaderRepository;
            _cTokenRecordReaderRepository = cTokenRecordReaderRepository;
            _tokenRateCalculateService = tokenRateCalculateService;
            _chainAppService = chainAppService;
            _debitAppPriceService = debitAppPriceService;
            _mainToken = debitOptionsSnapshot.Value.MainToken;
            _logger = logger;
        }

        public async Task<ListResultDto<CompControllerDto>> GetCompControllerListAsync(GetCompControllerInput input)
        {
            var compControllers =
                (await _compControllerReaderRepository.GetListAsync(GetCompControllerFilterQueryContainer(input)))
                .Item2;
            return new ListResultDto<CompControllerDto>
            {
                Items = ObjectMapper.Map<List<CompController>, List<CompControllerDto>>(compControllers)
            };
        }

        public async Task<ListResultDto<CTokenDto>> GetCTokenListAsync(GetCTokenListInput input)
        {
            List<Guid> cTokenIdList = null;
            if (!string.IsNullOrEmpty(input.User))
            {
                var userList = (await GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput
                {
                    CompControllerId = input.CompControllerId,
                    ChainId = input.ChainId,
                    CTokenId = input.CTokenId,
                    User = input.User
                })).Items.ToList();
                cTokenIdList = userList.Select(x => x.CTokenInfo.Id).ToList();
                if (!cTokenIdList.Any())
                {
                    return new ListResultDto<CTokenDto>();
                }
            }

            var cTokens =
                (await _cTokenReaderRepository.GetListAsync(GetCTokenFilterQueryContainer(input, cTokenIdList),
                    limit: DebitConstants.DefaultSize))
                .Item2;
            var cTokensDtos = ObjectMapper.Map<List<CToken>, List<CTokenDto>>(cTokens);
            if (input.IsWithUnderlyingTokenPrice)
            {
                await ModifyCTokenUnderlyingTokenPriceAsync(cTokensDtos);
            }

            if (!input.IsWithApy)
            {
                return new ListResultDto<CTokenDto>
                {
                    Items = cTokensDtos
                };
            }

            return new ListResultDto<CTokenDto>
            {
                Items = await ModifyCTokensApyAsync(cTokensDtos)
            };
        }

        public async Task<ListResultDto<CTokenUserInfoDto>> GetCTokenUserInfoListAsync(GetCTokenUserInfoInput input)
        {
            var userInfos = (await _cTokenUserInfoReaderRepository.GetListAsync(
                GetUserFilterQueryContainer(input)
                , limit: DebitConstants.DefaultUserInfoSize)).Item2;
            var userDtoInfos = ObjectMapper.Map<List<CTokenUserInfo>, List<CTokenUserInfoDto>>(userInfos);
            return new ListResultDto<CTokenUserInfoDto>
            {
                Items = userDtoInfos
            };
        }

        public async Task<PagedResultDto<CTokenRecordDto>> GetCTokenRecordListAsync(GetCTokenRecordInput input)
        {
            var skipCount = input.SkipCount > 0 ? input.SkipCount : 0;
            var size = input.Size > 0 ? input.Size : DebitConstants.DefaultRecordSize;
            var totalCount = await _cTokenRecordReaderRepository.CountAsync(GetRecordFilterQueryContainer(input));
            var records = (await _cTokenRecordReaderRepository.GetListAsync(GetRecordFilterQueryContainer(input),
                null,
                x => x.Date,
                input.IsAscend ? SortOrder.Ascending : SortOrder.Descending,
                size,
                skipCount
            )).Item2;
            return new PagedResultDto<CTokenRecordDto>
            {
                Items = ObjectMapper.Map<List<CTokenRecord>, List<CTokenRecordDto>>(records),
                TotalCount = totalCount.Count
            };
        }
        
        private async Task<List<CTokenDto>> ModifyCTokensApyAsync(List<CTokenDto> cTokensDtos)
        {
            var cTokenPriceList = await GetCTokenPriceAsync(cTokensDtos);
            var chainInfoDic = new Dictionary<string, ChainDto>();
            var compPriceDic = new Dictionary<string, decimal>();
            var chains = (await _chainAppService.GetListAsync(new GetChainInput())).Items;
            foreach (var cTokensDto in cTokensDtos)
            {
                compPriceDic.TryGetValue(cTokensDto.ChainId, out var compPrice);
                if (!chainInfoDic.TryGetValue(cTokensDto.ChainId, out var chain))
                {
                    chain = chains.First(x => x.Id == cTokensDto.ChainId);
                    chainInfoDic.TryAdd(cTokensDto.ChainId, chain);
                    compPrice = decimal.Parse(await _debitAppPriceService.GetTokenPricesAsync(new GetTokenPriceInput
                    {
                        ChainId = cTokensDto.ChainId,
                        Symbol = _mainToken
                    }));
                    compPriceDic.TryAdd(cTokensDto.ChainId, compPrice);
                }

                if (cTokenPriceList.TryGetValue(
                    GetCTokenKey(cTokensDto.ChainId, cTokensDto.Address,
                        cTokensDto.Symbol),
                    out var cTokenPrice))
                {
                    await CalculateStatisticInfoAsync(cTokensDto, compPrice, cTokenPrice, chain.BlocksPerDay);
                }
            }

            return cTokensDtos;
        }

        private Func<QueryContainerDescriptor<CompController>, QueryContainer> GetCompControllerFilterQueryContainer(
            GetCompControllerInput input)
        {
            return q =>
            {
                if (input.CompControllerId.HasValue)
                {
                    return q
                        .Term(t => t
                            .Field(f => f.Id).Value(input.CompControllerId.Value));
                }

                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    return q
                        .Term(i => i
                            .Field(f => f.ChainId).Value(input.ChainId));
                }

                return null;
            };
        }

        private async Task<decimal> GetTokenPriceAsync(string chainId, string tokenAddress, string tokenSymbol)
        {
            var tokenPriceDto = await _debitAppPriceService.GetTokenPricesAsync(new GetTokenPriceInput
            {
                TokenAddress = tokenAddress, ChainId = chainId, Symbol = tokenSymbol
            });
            return decimal.Parse(tokenPriceDto);
        }

        private async Task CalculateStatisticInfoAsync(CTokenDto cTokenDto, decimal compPrice, decimal cTokenPrice,
            int blocksPerDay)
        {
            var borrow = BigInteger.Parse(cTokenDto.TotalUnderlyingAssetBorrowAmount);
            var reserve = BigInteger.Parse(cTokenDto.TotalUnderlyingAssetReserveAmount);
            var cash = BigInteger.Parse(cTokenDto.TotalUnderlyingAssetAmount) - borrow;
        
            var borrowRate = _tokenRateCalculateService.GetBorrowRate(cTokenDto, cash,
                borrow, reserve);
            var reserveFactorMantissa = BigInteger.Parse(cTokenDto.ReserveFactorMantissa);
            var supplyRate = _tokenRateCalculateService.GetSupplyRate(cTokenDto, cash,
                borrow, reserve, reserveFactorMantissa);
            cTokenDto.BorrowInterest = StatisticCalculationHelper.GetYearInterest(blocksPerDay, borrowRate);
            cTokenDto.SupplyInterest = StatisticCalculationHelper.GetYearInterest(blocksPerDay, supplyRate);
            var underlyingTokenPrice = cTokenDto.UnderlyingToken.TokenPrice != 0m
                ? cTokenDto.UnderlyingToken.TokenPrice
                : await GetTokenPriceAsync(cTokenDto.ChainId, cTokenDto.UnderlyingToken.Address, cTokenDto.UnderlyingToken.Symbol);
            var tokenTotalValue = BigDecimal.Parse(cTokenDto.TotalCTokenMintAmount) * cTokenPrice;
            var underlyingTokenTotalValue =
                BigDecimal.Parse(cTokenDto.TotalUnderlyingAssetAmount) * underlyingTokenPrice;
            cTokenDto.BorrowApy =
                StatisticCalculationHelper.CalculateApy(borrowRate, compPrice, underlyingTokenTotalValue, blocksPerDay);
            cTokenDto.SupplyApy =
                StatisticCalculationHelper.CalculateApy(supplyRate, compPrice, tokenTotalValue, blocksPerDay);
        }

        private Func<QueryContainerDescriptor<CToken>, QueryContainer> GetCTokenFilterQueryContainer(
            GetCTokenListInput input, IReadOnlyCollection<Guid> tokenIds = null)
        {
            return q =>
            {
                QueryContainer totalQueryContainer = null;
                if (tokenIds != null && tokenIds.Any())
                {
                    totalQueryContainer = q
                        .Terms(i => i
                            .Field(f => f.Id)
                            .Terms(tokenIds));
                }

                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    return q
                        .Term(i => i
                            .Field(f => f.ChainId).Value(input.ChainId)) && totalQueryContainer;
                }

                if (input.CompControllerId.HasValue)
                {
                    return q
                        .Term(i => i
                            .Field(f => f.CompControllerId).Value(input.CompControllerId.Value)) && totalQueryContainer;
                }

                if (input.CTokenId.HasValue)
                {
                    return q
                        .Term(i => i
                            .Field(f => f.Id).Value(input.CTokenId.Value));
                }

                return totalQueryContainer;
            };
        }


        private Func<QueryContainerDescriptor<CTokenRecord>, QueryContainer> GetRecordFilterQueryContainer(
            GetCTokenRecordInput input)
        {
            return q =>
            {
                QueryContainer totalQueryContainer = q
                    .Term(t => t
                        .Field(f => f.User).Value(input.User));
                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.CompControllerInfo.ChainId).Value(input.ChainId));
                }

                if (input.CompControllerId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.CompControllerInfo.Id).Value(input.CompControllerId.Value));
                }

                if (input.StartTime > 0)
                {
                    var startTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.StartTime);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.Date)
                            .GreaterThanOrEquals(startTimeDate));
                }

                if (input.EndTime > 0)
                {
                    var endTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.EndTime);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.Date)
                            .LessThanOrEquals(endTimeDate));
                }

                if (input.BehaviorType.HasValue)
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(t => t
                            .Field(f => f.BehaviorType).Value(input.BehaviorType.Value));
                }

                if (input.CTokenId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer && q
                        .Term(t => t
                            .Field(f => f.CToken.Id).Value(input.CTokenId.Value));
                }

                return totalQueryContainer;
            };
        }

        private Func<QueryContainerDescriptor<CTokenUserInfo>, QueryContainer> GetUserFilterQueryContainer(
            GetCTokenUserInfoInput input)
        {
            return q =>
            {
                QueryContainer totalQueryContainer = null;
                if (!string.IsNullOrEmpty(input.User))
                {
                    totalQueryContainer = q
                        .Term(i => i
                            .Field(f => f.User).Value(input.User));
                }

                if (!string.IsNullOrEmpty(input.ChainId))
                {
                    return totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.ChainId).Value(input.ChainId));
                }

                if (input.CompControllerId.HasValue)
                {
                    return totalQueryContainer && q
                        .Term(i => i
                            .Field(f => f.CompInfo.Id).Value(input.CompControllerId.Value));
                }

                if (input.CTokenId.HasValue)
                {
                    return totalQueryContainer && q
                        .Term(t => t
                            .Field(f => f.CTokenInfo.Id).Value(input.CTokenId.Value));
                }

                return totalQueryContainer;
            };
        }

        private async Task<Dictionary<string, decimal>> GetCTokenPriceAsync(List<CTokenDto> cTokenDtoList)
        {
            var ret = new Dictionary<string, decimal>();
            foreach (var chainCTokensInfo in cTokenDtoList.GroupBy(x => x.ChainId))
            {
                var chainId = chainCTokensInfo.Key;
                var cTokenPrices = await _debitAppPriceService.GetCTokenPricesAsync(new GetTokenPricesInput
                {
                    ChainId = chainId,
                    TokenAddresses = chainCTokensInfo.Select(t => t.Address).ToArray(),
                    TokenSymbol = chainCTokensInfo.Select(t => t.Symbol).ToArray()
                });

                foreach (var cTokenPrice in cTokenPrices.Where(cTokenPrice => !ret.TryAdd(
                    GetCTokenKey(chainId, cTokenPrice.TokenAddress, cTokenPrice.TokenSymbol),
                    decimal.Parse(cTokenPrice.Price))))
                {
                    _logger.LogWarning(
                        $"Failed to add token price; chain Id: {chainId}, token address: {cTokenPrice.TokenAddress}, token symbol: {cTokenPrice.TokenSymbol}");
                }
            }

            return ret;
        }
        
        private async Task ModifyCTokenUnderlyingTokenPriceAsync(List<CTokenDto> cTokenDtoList)
        {
            var priceDic = new Dictionary<string, decimal>();
            foreach (var chainCTokensInfo in cTokenDtoList.GroupBy(x => x.ChainId))
            {
                var chainId = chainCTokensInfo.Key;
                var cTokenUnderlyingTokenPrices = await _debitAppPriceService.GetUnderlyingTokenPricesAsync(new GetTokenPricesInput
                {
                    ChainId = chainId,
                    TokenAddresses = chainCTokensInfo.Select(t => t.UnderlyingToken.Address).ToArray(),
                    TokenSymbol = chainCTokensInfo.Select(t => t.UnderlyingToken.Symbol).ToArray()
                });

                foreach (var cTokenUnderlyingPrice in cTokenUnderlyingTokenPrices.Where(cTokenUnderlyingTokenPrice =>
                    !priceDic.TryAdd(
                        GetCTokenKey(chainId, cTokenUnderlyingTokenPrice.TokenAddress,
                            cTokenUnderlyingTokenPrice.TokenSymbol),
                        decimal.Parse(cTokenUnderlyingTokenPrice.Price))))
                {
                    _logger.LogWarning(
                        $"Failed to add token price; chain Id: {chainId}, token address: {cTokenUnderlyingPrice.TokenAddress}, token symbol: {cTokenUnderlyingPrice.TokenSymbol}");
                }

                foreach (var cTokenDto in cTokenDtoList)
                {
                    if (priceDic.TryGetValue(
                        GetCTokenKey(cTokenDto.ChainId, cTokenDto.UnderlyingToken.Address,
                            cTokenDto.UnderlyingToken.Symbol), out var price))
                    {
                        cTokenDto.UnderlyingToken.TokenPrice = price;
                    }
                }
            }
        }

        private string GetCTokenKey(string chainId, string tokenAddress, string tokenSymbol)
        {
            return $"{chainId.ToString()}{tokenAddress ?? string.Empty}{tokenSymbol ?? string.Empty}";
        }

        private bool IsCTokenDeprecated(CToken cToken)
        {
            return cToken.CollateralFactorMantissa == DebitConstants.ZeroBalance &&
                   cToken.IsBorrowPaused &&
                   cToken.ReserveFactorMantissa == DebitConstants.Mantissa;
        }
    }
}