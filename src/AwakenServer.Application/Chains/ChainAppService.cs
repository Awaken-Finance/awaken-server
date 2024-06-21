using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.Chain;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Chains
{
    [RemoteService(IsEnabled = false)]
    public class ChainAppService : AwakenServerAppService, IChainAppService
    {
        private readonly INESTRepository<Chain, string> _chainIndexRepository;
        private readonly IBlockchainAppService _blockchainAppService;
        private readonly IObjectMapper _objectMapper;
        private readonly IClusterClient _clusterClient;
        private readonly IDistributedEventBus _distributedEventBus;
        private static readonly ConcurrentDictionary<string, ChainDto> ChainCache = new ();

        public ChainAppService(INESTRepository<Chain, string> chainIndexRepository,
            IBlockchainAppService blockchainAppService,
            IObjectMapper objectMapper, IClusterClient clusterClient, IDistributedEventBus distributedEventBus)
        {
            _chainIndexRepository = chainIndexRepository;
            _blockchainAppService = blockchainAppService;
            _objectMapper = objectMapper;
            _clusterClient = clusterClient;
            _distributedEventBus = distributedEventBus;
        }

        public async Task<ChainDto> GetChainAsync(string chainId)
        {
            var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
            var chainGrainDto = await chainGrain.GetByIdAsync(chainId);
            if (chainGrainDto != null && !chainGrainDto.IsEmpty())
            {
                return _objectMapper.Map<ChainGrainDto, ChainDto>(chainGrainDto);
            }

            var chain = await _chainIndexRepository.GetAsync(chainId);
            if (chain == null)
            {
                return null;
            }

            await chainGrain.AddChainAsync(_objectMapper.Map<Chain, ChainGrainDto>(chain));
            return _objectMapper.Map<Chain, ChainDto>(chain);
        }

        public async Task<ListResultDto<ChainDto>> GetListAsync(GetChainInput getChainInput)
        {
            var items = await _chainIndexRepository.GetListAsync();
            var chainDtoList = _objectMapper.Map<List<Chain>, List<ChainDto>>(items.Item2);
            if (!getChainInput.IsNeedBlockHeight)
            {
                return new ListResultDto<ChainDto>
                {
                    Items = chainDtoList
                };
            }

            foreach (var chainDto in chainDtoList)
            {
                var latestBlockHeight = (await GetChainStatusAsync(chainDto.Id)).LatestBlockHeight;
                chainDto.LatestBlockHeight = latestBlockHeight;
            }

            return new ListResultDto<ChainDto>
            {
                Items = chainDtoList
            };
        }

        public async Task<ChainDto> GetByNameCacheAsync(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            if (ChainCache.TryGetValue(name, out var chainDto))
            {
                return chainDto;
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<Chain>, QueryContainer>>();
            mustQuery.Add(q => q.Term(t => t.Field(f => f.Name).Value(name)));
            QueryContainer Filter(QueryContainerDescriptor<Chain> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _chainIndexRepository.GetListAsync(Filter);
            var items = _objectMapper.Map<List<Chain>, List<ChainDto>>(list.Item2);

            if (items.Count > 0 && !items.First().IsEmpty())
            {
                ChainCache.TryAdd(name, items.First());
                return items.First();
            }

            return null;
        }

        public async Task<ChainDto> GetByChainIdCacheAsync(string chainId)
        {
            if (String.IsNullOrEmpty(chainId))
            {
                return null;
            }

            if (ChainCache.TryGetValue(chainId, out var chainDto))
            {
                return chainDto;
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<Chain>, QueryContainer>>();
            mustQuery.Add(q => q.Term(t => t.Field(f => f.AElfChainId).Value(chainId)));
            QueryContainer Filter(QueryContainerDescriptor<Chain> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _chainIndexRepository.GetListAsync(Filter);
            var items = _objectMapper.Map<List<Chain>, List<ChainDto>>(list.Item2);

            if (items.Count > 0)
            {
                if (!items.First().IsEmpty())
                {
                    ChainCache.TryAdd(chainId, items.First());
                    return items.First();
                }
            }

            return items.FirstOrDefault();
        }

        public async Task<ChainDto> UpdateAsync(ChainUpdateDto input)
        {
            if (string.IsNullOrEmpty(input.Id))
            {
                return null;
            }

            var chainGrain = _clusterClient.GetGrain<IChainGrain>(input.Id);
            if (input.AElfChainId.HasValue)
            {
                await chainGrain.SetChainIdAsync(input.AElfChainId.Value);
            }

            if (input.Name != null)
            {
                await chainGrain.SetNameAsync(input.Name);
            }

            if (input.LatestBlockHeight.HasValue)
            {
                if (input.LatestBlockHeightExpireMs.HasValue)
                {
                    await chainGrain.SetBlockHeightAsync(input.LatestBlockHeight.Value,
                        input.LatestBlockHeightExpireMs.Value);
                }
                else
                {
                    await chainGrain.SetBlockHeightAsync(input.LatestBlockHeight.Value, 0);
                }
            }

            return _objectMapper.Map<ChainGrainDto, ChainDto>(await chainGrain.GetByIdAsync(input.Id));
        }

        public async Task<ChainStatusDto> GetChainStatusAsync(string chainId)
        {
            var chainGrain = _clusterClient.GetGrain<IChainGrain>(chainId);
            var chainGrainDto = await chainGrain.GetByIdAsync(chainId);
            if (chainGrainDto == null)
            {
                return null;
            }
/*
            var latestBlockHeightExpireMs = chainGrainDto.LatestBlockHeightExpireMs ?? 0;
            var timeDiff = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - latestBlockHeightExpireMs;
            if (chainGrainDto.LatestBlockHeight > 0 && timeDiff < 0)
            {
                return new ChainStatusDto
                {
                    LatestBlockHeight = chainGrainDto.LatestBlockHeight
                };
            }
            
            var latestBlockHeight = await _blockchainAppService.GetBlockNumberAsync(chainGrainDto.Name);
            await chainGrain.SetBlockHeightAsync(latestBlockHeight, DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds());
*/
            return new ChainStatusDto
            {
                LatestBlockHeight = 0
            };
        }
    }
}