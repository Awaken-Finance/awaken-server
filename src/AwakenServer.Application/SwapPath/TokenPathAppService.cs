using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.SwapTokenPath;
using AwakenServer.Grains.Grain.Price;
using AwakenServer.SwapTokenPath.Dtos;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using AwakenServer.Trade.Index;
using TradePair = AwakenServer.Trade.Index.TradePair;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace AwakenServer.SwapTokenPath
{
    [RemoteService(IsEnabled = false)]
    public class TokenPathAppService : ApplicationService, ITokenPathAppService
    {
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly ILogger<TradePairAppService> _logger;
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
        
        public TokenPathAppService(
            ILogger<TradePairAppService> logger,
            IClusterClient clusterClient,
            IObjectMapper objectMapper,
            INESTRepository<TradePair, Guid> tradePairIndexRepository)
        {
            _logger = logger;
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _tradePairIndexRepository = tradePairIndexRepository;
        }
        
        private async Task<List<TradePairDto>> GetListAsync(string chainId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tradePairIndexRepository.GetListAsync(Filter);
            return _objectMapper.Map<List<TradePair>, List<TradePairDto>>(list.Item2);
        }
        
        public async Task<PagedResultDto<TokenPathDto>> GetListAsync(GetTokenPathsInput input)
        {
            _logger.LogInformation($"get token paths begin, input: {input.ChainId}, {input.StartSymbol}, {input.EndSymbol}, {input.MaxDepth}");

            var pairs = await GetListAsync(input.ChainId);
            
            _logger.LogInformation($"get token paths, get relations from chain trade pairs, count: {pairs.Count}");
            
            var grain = _clusterClient.GetGrain<ITokenPathGrain>(GrainIdHelper.GenerateGrainId(input.ChainId));
            await grain.SetGraphAsync(new GraphDto()
            {
                Relations = pairs
            });

            var result =
                await grain.GetPathAsync(_objectMapper.Map<GetTokenPathsInput, GetTokenPathGrainDto>(input));
            
            if (!result.Success || result.Data == null || result.Data.Path == null)
            {
                _logger.LogError($"get token paths, failed, flag: {result.Success}");
                return new PagedResultDto<TokenPathDto>();
            }
            
            _logger.LogInformation($"get token paths done, path count: {result.Data.Path.Count}");
            return new PagedResultDto<TokenPathDto>()
            {
                TotalCount = result.Data.Path.Count,
                Items = _objectMapper.Map<List<TokenPath>, List<TokenPathDto>>(result.Data.Path)
            };
        }
    }
}