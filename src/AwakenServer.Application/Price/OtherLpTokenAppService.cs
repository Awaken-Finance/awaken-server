using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Dtos;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.Price
{
    [RemoteService(IsEnabled = false)]
    public class OtherLpTokenAppService: ApplicationService, IOtherLpTokenAppService
    {
        private readonly IOtherLpTokenRepository _otherLpTokenRepository;
        private readonly INESTReaderRepository<Index.OtherLpToken, Guid> _otherLpTokenIndexReaderRepository;

        public OtherLpTokenAppService(IOtherLpTokenRepository otherLpTokenRepository, 
            INESTReaderRepository<Index.OtherLpToken, Guid> otherLpTokenIndexReaderRepository)
        {
            _otherLpTokenRepository = otherLpTokenRepository;
            _otherLpTokenIndexReaderRepository = otherLpTokenIndexReaderRepository;
        }
        
        public async Task CreateAsync(OtherLpTokenCreateDto input)
        {
            await _otherLpTokenRepository.InsertAsync(ObjectMapper.Map<OtherLpTokenCreateDto, OtherLpToken>(input));
        }

        public async Task UpdateAsync(OtherLpTokenDto input)
        {
            await _otherLpTokenRepository.UpdateAsync(ObjectMapper.Map<OtherLpTokenDto, OtherLpToken>(input));
        }

        public async Task<OtherLpTokenDto> GetByAddressAsync(string chainId, string address)
        {
            var otherLpToken =
                await _otherLpTokenRepository.FirstOrDefaultAsync(o => o.ChainId == chainId && o.Address == address);
            return otherLpToken == null ? null : ObjectMapper.Map<OtherLpToken, OtherLpTokenDto>(otherLpToken);
        }

        public async Task<List<OtherLpTokenIndexDto>> GetOtherLpTokenIndexListAsync(string chainId, IEnumerable<string> addresses)
        {
            QueryContainer Filter(QueryContainerDescriptor<Index.OtherLpToken> q) =>
                q.Term(i => i.Field(f => f.ChainId).Value(chainId)) &&
                q.Terms(i => i.Field(f => f.Address).Terms(addresses));
            var list = await _otherLpTokenIndexReaderRepository.GetListAsync(Filter,
                limit: addresses.Count(), skip: 0);
            return ObjectMapper.Map<List<Index.OtherLpToken>, List<OtherLpTokenIndexDto>>(list.Item2);
        }
    }
}