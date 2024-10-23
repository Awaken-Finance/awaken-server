using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.SwapTokenPath;
using AwakenServer.SwapTokenPath.Dtos;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Index;
using Nest;
using Orleans;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;
using TradePair = AwakenServer.Trade.Index.TradePair;

namespace AwakenServer.SwapTokenPath
{
    [RemoteService(IsEnabled = false)]
    public class TokenPathAppService : ApplicationService, ITokenPathAppService
    {
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private readonly ILogger _logger;
        private readonly INESTRepository<TradePair, Guid> _tradePairIndexRepository;
        
        public TokenPathAppService(
            IClusterClient clusterClient,
            IObjectMapper objectMapper,
            INESTRepository<TradePair, Guid> tradePairIndexRepository)
        {
            _logger = Log.ForContext<TokenPathAppService>();
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _tradePairIndexRepository = tradePairIndexRepository;
        }
        
        private async Task<List<TradePairWithToken>> GetListAsync(string chainId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<TradePair>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));
            QueryContainer Filter(QueryContainerDescriptor<TradePair> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tradePairIndexRepository.GetListAsync(Filter);
            return _objectMapper.Map<List<TradePair>, List<TradePairWithToken>>(list.Item2);
        }
        
        public async Task<PagedResultDto<TokenPathDto>> GetListAsync(GetTokenPathsInput input)
        {
            _logger.Information($"get token paths begin, input: {input.ChainId}, {input.StartSymbol}, {input.EndSymbol}, {input.MaxDepth}");
            
            var grain = _clusterClient.GetGrain<ITokenPathGrain>(input.ChainId);
            
            var cachedResult = await grain.GetCachedPathAsync(_objectMapper.Map<GetTokenPathsInput, GetTokenPathGrainDto>(input));
            if (cachedResult.Success)
            {
                _logger.Information($"get token paths from cache done, path count: {cachedResult.Data.Path.Count}");
                
                return new PagedResultDto<TokenPathDto>()
                {
                    TotalCount = cachedResult.Data.Path.Count,
                    Items = _objectMapper.Map<List<TokenPath>, List<TokenPathDto>>(cachedResult.Data.Path)
                };
            }
            
            var pairs = await GetListAsync(input.ChainId);
            
            _logger.Information($"get token paths do search, get relations from chain trade pairs, count: {pairs.Count}");
            
            await grain.SetGraphAsync(new GraphDto()
            {
                Relations = _objectMapper.Map<List<TradePairWithToken>, List<TradePairWithTokenDto>>(pairs)
            });

            var result =
                await grain.GetPathAsync(_objectMapper.Map<GetTokenPathsInput, GetTokenPathGrainDto>(input));
            
            if (!result.Success || result.Data == null || result.Data.Path == null)
            {
                _logger.Error($"get token paths, failed, flag: {result.Success}");
                return new PagedResultDto<TokenPathDto>();
            }
            
            _logger.Information($"get token paths done, path count: {result.Data.Path.Count}");
            return new PagedResultDto<TokenPathDto>()
            {
                TotalCount = result.Data.Path.Count,
                Items = _objectMapper.Map<List<TokenPath>, List<TokenPathDto>>(result.Data.Path)
            };
        }
    }
}