using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.GameOfTrust.DTos.Dto;
using AwakenServer.IDO.Dtos;
using AwakenServer.IDO.Entities.Es;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.IDO
{
    [RemoteService(IsEnabled = false)]
    public class IdoAppService : AwakenServerAppService, IIdoAppService
    {
        private readonly INESTRepository<PublicOffering, Guid> _esPublicOfferingRepository;
        private readonly INESTRepository<UserPublicOffering, Guid> _esUserPublicOfferingRepository;
        private readonly INESTRepository<PublicOfferingRecord, Guid> _esUserRecordRepository;

        public IdoAppService(INESTRepository<PublicOffering, Guid> esPublicOfferingRepository,
            INESTRepository<UserPublicOffering, Guid> esUserPublicOfferingRepository,
            INESTRepository<PublicOfferingRecord, Guid> esUserRecordRepository)
        {
            _esPublicOfferingRepository = esPublicOfferingRepository;
            _esUserPublicOfferingRepository = esUserPublicOfferingRepository;
            _esUserRecordRepository = esUserRecordRepository;
        }

        public async Task<PagedResultDto<PublicOfferingDto>> GetPublicOfferingsAsync(GetPublicOfferingInput input)
        {
            var (skipCount, size) = GetPageCountInfo(input.SkipCount, input.Size);
            var totalCountInfo =
                await _esPublicOfferingRepository.CountAsync(
                    GetPublicOfferingFilterQueryContainer(input.ChainId));
            var publicOfferings = (await _esPublicOfferingRepository.GetListAsync(
                    GetPublicOfferingFilterQueryContainer(input.ChainId), null, x => x.OrderRank, SortOrder.Ascending,
                    size,
                    skipCount))
                .Item2;
            return new PagedResultDto<PublicOfferingDto>
            {
                Items = ObjectMapper.Map<List<PublicOffering>, List<PublicOfferingDto>>(publicOfferings),
                TotalCount = totalCountInfo.Count
            };
        }

        public async Task<PagedResultDto<UserPublicOfferingDto>> GetUserPublicOfferingsAsync(
            GetUserPublicOfferingInfoInput input)
        {
            var (skipCount, size) = GetPageCountInfo(input.SkipCount, input.Size);
            var totalCountInfo =
                await _esUserPublicOfferingRepository.CountAsync(
                    GetUserPublicOfferingFilterQueryContainer(input.ChainId, input.User));
            var userPublicOfferings = (await _esUserPublicOfferingRepository.GetListAsync(
                    GetUserPublicOfferingFilterQueryContainer(input.ChainId, input.User), null, null,
                    SortOrder.Ascending, size,
                    skipCount))
                .Item2;
            return new PagedResultDto<UserPublicOfferingDto>
            {
                Items =
                    ObjectMapper.Map<List<UserPublicOffering>, List<UserPublicOfferingDto>>(userPublicOfferings),
                TotalCount = totalCountInfo.Count
            };
        }

        public async Task<UserAssetDto> GetUserPublicOfferingsAssetAsync(GetUserAssetInput input)
        {
            var userPublicOfferings = (await _esUserPublicOfferingRepository.GetListAsync(
                GetUserPublicOfferingFilterQueryContainer(input.ChainId, input.User))).Item2;
            var totalUsdtValue = 0m;
            foreach (var userPublicOfferingInfo in userPublicOfferings)
            {
                var tokenInfo = userPublicOfferingInfo.PublicOfferingInfo.Token;
                var tokenPrice = await GetTokenUsdtPriceAsync(tokenInfo.Symbol);
                totalUsdtValue += userPublicOfferingInfo.TokenAmount * tokenPrice /
                                  (decimal) BigInteger.Pow(10, tokenInfo.Decimals);
            }

            var totalBtcValue = await GetUsdtBtcPriceAsync(totalUsdtValue);
            return new UserAssetDto
            {
                UsdtValue = totalUsdtValue,
                BtcValue = totalBtcValue
            };
        }

        public async Task<PublicOfferingAssetDto> GetPublicOfferingsTokensAsync(GetAssetTokenInfoInput input)
        {
            var publicOfferings = (await _esPublicOfferingRepository.GetListAsync(
                    GetPublicOfferingFilterQueryContainer(input.ChainId)))
                .Item2;
            var tokenComparer = new TokenEqualityComparer();
            var raiseTokenList = publicOfferings.Select(x =>
                x.RaiseToken).Distinct(tokenComparer).ToList();
            var tokenList = publicOfferings.Select(x =>
                x.Token).Distinct(tokenComparer).ToList();
            return new PublicOfferingAssetDto
            {
                Token = ObjectMapper.Map<List<Tokens.Token>, List<TokenDto>>(tokenList),
                RaiseToken = ObjectMapper.Map<List<Tokens.Token>, List<TokenDto>>(raiseTokenList)
            };
        }

        public async Task<PagedResultDto<PublicOfferingRecordDto>> GetPublicOfferingRecordsAsync(
            GetUserRecordInput input)
        {
            var (skipCount, size) = GetPageCountInfo(input.SkipCount, input.Size);
            var totalCountInfo =
                await _esUserRecordRepository.CountAsync(
                    GetUserPublicOfferingRecordFilterQueryContainer(input));
            var userPublicOfferings = (await _esUserRecordRepository.GetListAsync(
                    GetUserPublicOfferingRecordFilterQueryContainer(input), null, x => x.DateTime, SortOrder.Descending,
                    size,
                    skipCount))
                .Item2;
            return new PagedResultDto<PublicOfferingRecordDto>
            {
                TotalCount = totalCountInfo.Count,
                Items = ObjectMapper.Map<List<PublicOfferingRecord>, List<PublicOfferingRecordDto>>(userPublicOfferings)
            };
        }

        private (int, int) GetPageCountInfo(int skipCount, int size)
        {
            return (skipCount > 0 ? skipCount : 0,
                size > 0 ? size : IDOConstants.DefaultRecordSize);
        }

        private Func<QueryContainerDescriptor<PublicOffering>, QueryContainer> GetPublicOfferingFilterQueryContainer(
            string? chainId)
        {
            return q =>
            {
                if (!string.IsNullOrEmpty(chainId))
                {
                    return q
                        .Term(t => t
                            .Field(f => f.ChainId).Value(chainId));
                }

                return null;
            };
        }

        private Func<QueryContainerDescriptor<UserPublicOffering>, QueryContainer>
            GetUserPublicOfferingFilterQueryContainer(
                string chainId, string user)
        {
            return q =>
            {
                return q
                           .Term(t => t
                               .Field(f => f.ChainId).Value(chainId)) &&
                       q
                           .Term(t => t
                               .Field(f => f.User).Value(user));
            };
        }

        private Func<QueryContainerDescriptor<PublicOfferingRecord>, QueryContainer>
            GetUserPublicOfferingRecordFilterQueryContainer(
                GetUserRecordInput input)
        {
            return q =>
            {
                var totalQueryContainer = q
                                              .Term(t => t
                                                  .Field(f => f.ChainId).Value(input.ChainId)) &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.User).Value(input.User));

                if (input.TimestampMax > 0)
                {
                    var endTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMax);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.DateTime)
                            .LessThanOrEquals(endTimeDate));
                }

                if (input.TimestampMin > 0)
                {
                    var startTimeDate = DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMin);
                    totalQueryContainer = totalQueryContainer && +q
                        .DateRange(r => r
                            .Field(f => f.DateTime)
                            .GreaterThanOrEquals(startTimeDate));
                }

                if (input.TokenId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.PublicOfferingInfo.Token.Id)
                                                  .Value(input.TokenId.Value));
                }

                if (input.RaiseTokenId.HasValue)
                {
                    totalQueryContainer = totalQueryContainer &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.PublicOfferingInfo.RaiseToken.Id)
                                                  .Value(input.RaiseTokenId.Value));
                }

                if (input.OperateType > 0)
                {
                    totalQueryContainer = totalQueryContainer &&
                                          q
                                              .Term(t => t
                                                  .Field(f => f.OperateType)
                                                  .Value(input.OperateType));
                }

                return totalQueryContainer;
            };
        }

        private async Task<decimal> GetTokenUsdtPriceAsync(string token)
        {
            return 1;
        }

        private async Task<decimal> GetUsdtBtcPriceAsync(decimal usdtValue)
        {
            return 1m * usdtValue;
        }
    }
}