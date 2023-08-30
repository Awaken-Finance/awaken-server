using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Dtos;
using AwakenServer.Price.Index;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Price
{
    [RemoteService(IsEnabled = false)]
    public class LendingTokenPriceAppService : ApplicationService, ILendingTokenPriceAppService
    {
        private readonly ILendingTokenPriceRepository _lendingTokenPriceRepository;
        private readonly INESTReaderRepository<Index.LendingTokenPrice, Guid> _lendingTokenPriceIndexReaderRepository;
        private readonly INESTRepository<LendingTokenPriceHistory, Guid> _lendingTokenPriceHistoryIndexReaderRepository;

        public LendingTokenPriceAppService(ILendingTokenPriceRepository lendingTokenPriceRepository, 
            INESTReaderRepository<Index.LendingTokenPrice, Guid> lendingTokenPriceIndexReaderRepository,
            INESTRepository<LendingTokenPriceHistory, Guid> lendingTokenPriceHistoryIndexReaderRepository)
        {
            _lendingTokenPriceRepository = lendingTokenPriceRepository;
            _lendingTokenPriceIndexReaderRepository = lendingTokenPriceIndexReaderRepository;
            _lendingTokenPriceHistoryIndexReaderRepository = lendingTokenPriceHistoryIndexReaderRepository;
        }
        
        public async Task CreateOrUpdateAsync(LendingTokenPriceCreateOrUpdateDto input)
        {
            var lendingTokenPrice =
                await _lendingTokenPriceRepository.FirstOrDefaultAsync(l => l.ChainId == input.ChainId && l.TokenId == input.TokenId);
            if (lendingTokenPrice == null)
            {
                await _lendingTokenPriceRepository.InsertAsync(ObjectMapper.Map<LendingTokenPriceCreateOrUpdateDto, LendingTokenPrice>(input));
            }
            else
            {
                ObjectMapper.Map(input, lendingTokenPrice);
                await _lendingTokenPriceRepository.UpdateAsync(lendingTokenPrice);
            }
        }

        public async Task<LendingTokenPriceDto> GetByTokenIdAsync(Guid tokenId)
        {
            var lendingTokenPrice =
                await _lendingTokenPriceRepository.FirstOrDefaultAsync(l => l.TokenId == tokenId);
            return lendingTokenPrice == null
                ? null
                : ObjectMapper.Map<LendingTokenPrice, LendingTokenPriceDto>(lendingTokenPrice);
        }

        public async Task<List<LendingTokenPriceIndexDto>> GetPricesAsync(GetPricesInput input)
        {
            QueryContainer Filter(QueryContainerDescriptor<Index.LendingTokenPrice> q) =>
                q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)) &&
                q.Terms(i => i.Field(f => f.Token.Id).Terms(input.TokenIds));
            
            var list = await _lendingTokenPriceIndexReaderRepository.GetListAsync(Filter,
                limit: input.TokenIds.Length, skip: 0);

            return ObjectMapper.Map<List<Index.LendingTokenPrice>, List<LendingTokenPriceIndexDto>>(list.Item2);
        }
        
        public async Task<List<LendingTokenPriceIndexDto>> GetPricesAsync(string chainId, string[] tokenAddresses)
        {
            QueryContainer Filter(QueryContainerDescriptor<Index.LendingTokenPrice> q) =>
                q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
                q.Terms(i => i.Field(f => f.Token.Address).Terms(tokenAddresses));
            
            var list = await _lendingTokenPriceIndexReaderRepository.GetListAsync(Filter,
                limit: tokenAddresses.Length, skip: 0);

            return ObjectMapper.Map<List<Index.LendingTokenPrice>, List<LendingTokenPriceIndexDto>>(list.Item2);
        }
        
        public async Task<PagedResultDto<LendingTokenPriceHistoryIndexDto>> GetPriceHistoryAsync(GetPriceHistoryInput input)
        {
            QueryContainer Filter(QueryContainerDescriptor<LendingTokenPriceHistory> q) =>
                q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)) &&
                q.Term(i => i.Field(f => f.Token.Id).Value(input.TokenId)) && 
                q.DateRange(i => i.Field(f => f.Timestamp).GreaterThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMin))) &&
                q.DateRange(i=>i.Field(f=>f.Timestamp).LessThanOrEquals(DateTimeHelper.FromUnixTimeMilliseconds(input.TimestampMax)));

            var list = await _lendingTokenPriceHistoryIndexReaderRepository.GetListAsync(Filter,
                limit: input.MaxResultCount, skip: input.SkipCount);
            var totalCount = await _lendingTokenPriceHistoryIndexReaderRepository.CountAsync(Filter);

            return new PagedResultDto<LendingTokenPriceHistoryIndexDto>
            {
                Items = ObjectMapper.Map<List<LendingTokenPriceHistory>, List<LendingTokenPriceHistoryIndexDto>>(list.Item2),
                TotalCount = totalCount.Count
            };
        }
    }
}