using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.Tokens;
using Nest;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Tokens
{
    [RemoteService(IsEnabled = false)]
    public class TokenAppService : ApplicationService, ITokenAppService
    {
        private readonly INESTRepository<Token, Guid> _tokenIndexRepository;
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private static readonly ConcurrentDictionary<string, TokenDto> SymbolCache = new();
        private readonly IDistributedEventBus _distributedEventBus;

        public TokenAppService(INESTRepository<Token, Guid> tokenIndexRepository, IClusterClient clusterClient,
            IObjectMapper objectMapper, IDistributedEventBus distributedEventBus)
        {
            _tokenIndexRepository = tokenIndexRepository;
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _distributedEventBus = distributedEventBus;
        }

        public void DeleteAsync(Guid id)
        {
            //do nothing
            return ;
        }

        public TokenDto GetBySymbolCache(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return null;
            }

            return SymbolCache.TryGetValue(symbol, out var tokenDto) ? tokenDto : null;
        }

        public async Task<TokenDto> GetAsync(Guid id)
        {
            var tokenStateGrain =  _clusterClient.GetGrain<ITokenStateGrain>(id);
            var token = await tokenStateGrain.GetByIdAsync(id);
            if (token != null)
            {
                if (token.Success && !token.Data.IsEmpty())
                {
                    return _objectMapper.Map<TokenGrainDto, TokenDto>(token.Data);
                }
            }
            
            var tokenFromEs = await _tokenIndexRepository.GetAsync(id);
            if (tokenFromEs == null)
            {
                return null;
            }

            await tokenStateGrain.CreateAsync(_objectMapper.Map<Token,TokenCreateDto>(tokenFromEs));
            return _objectMapper.Map<Token, TokenDto>(tokenFromEs);
        }

        public async Task<TokenDto> GetAsync(GetTokenInput input)
        {
            //when input has id,go to the cache 1st
            if (input.Id != Guid.Empty)
            {
                var tokenStateGrain =  _clusterClient.GetGrain<ITokenStateGrain>(input.Id);
                var token = await tokenStateGrain.GetByIdAsync(input.Id);
                if (token != null)
                {
                    if (token.Success && !token.Data.IsEmpty())
                    {
                        return _objectMapper.Map<TokenGrainDto, TokenDto>(token.Data);
                    }
                }
            }
            var mustQuery = new List<Func<QueryContainerDescriptor<Token>, QueryContainer>>();
            if (input.Id != Guid.Empty)
            {
                mustQuery.Add(q => q.Term(t => t.Field(f => f.Id).Value(input.Id)));
            }
            if (!string.IsNullOrWhiteSpace(input.ChainId))
            {
                mustQuery.Add(q => q.Term(t => t.Field(f => f.ChainId).Value(input.ChainId)));
            }
            if (!string.IsNullOrWhiteSpace(input.Symbol))
            {
                mustQuery.Add(q => q.Term(t => t.Field(f => f.Symbol).Value(input.Symbol)));
            }
            if (!string.IsNullOrWhiteSpace(input.Address))
            {
                mustQuery.Add(q => q.Term(t => t.Field(f => f.Address).Value(input.Address)));
            }
            
            QueryContainer Filter(QueryContainerDescriptor<Token> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tokenIndexRepository.GetListAsync(Filter);
            var items = _objectMapper.Map<List<Token>, List<TokenDto>>(list.Item2);
            return items.FirstOrDefault();
        }

        public async Task<TokenDto> CreateAsync(TokenCreateDto input)
        {
            var token = _objectMapper.Map<TokenCreateDto, Token>(input);
            input.Id = (input.Id == Guid.Empty) ? Guid.NewGuid() : input.Id;
            token.Id = input.Id;

            //await _tokenIndexRepository.AddOrUpdateAsync(token);
            var tokenStateGrain = _clusterClient.GetGrain<ITokenStateGrain>(token.Id);
            var tokenGrainDto = await tokenStateGrain.CreateAsync(input);

            if (tokenGrainDto.Success)
            {
                await _distributedEventBus.PublishAsync(
                    _objectMapper.Map<TokenGrainDto, NewTokenEvent>(tokenGrainDto.Data));
            }
            
            var tokenDto = _objectMapper.Map<TokenGrainDto, TokenDto>(tokenGrainDto.Data);

            if (!string.IsNullOrWhiteSpace(tokenGrainDto.Data.Symbol))
            {
                SymbolCache.AddOrUpdate(tokenGrainDto.Data.Symbol, tokenDto, (_, existingTokenDto) => tokenDto);
            }

            return tokenDto;
        }
    }
}