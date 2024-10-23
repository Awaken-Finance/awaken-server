using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Tokens;
using Nest;
using Orleans;
using Serilog;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.Tokens
{
    [RemoteService(IsEnabled = false)]
    public class TokenAppService : ApplicationService, ITokenAppService
    {
        private readonly INESTRepository<TokenEntity, Guid> _tokenIndexRepository;
        private readonly IClusterClient _clusterClient;
        private readonly IObjectMapper _objectMapper;
        private static readonly ConcurrentDictionary<string, TokenDto> SymbolCache = new();
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly ILogger _logger;
        private readonly IAElfClientProvider _aelfClientProvider;


        public TokenAppService(INESTRepository<TokenEntity, Guid> tokenIndexRepository, 
            IClusterClient clusterClient,
            IObjectMapper objectMapper, 
            IDistributedEventBus distributedEventBus,
            IAElfClientProvider aelfClientProvider)
        {
            _tokenIndexRepository = tokenIndexRepository;
            _clusterClient = clusterClient;
            _objectMapper = objectMapper;
            _distributedEventBus = distributedEventBus;
            _logger = Log.ForContext<TokenAppService>();
            _aelfClientProvider = aelfClientProvider;
        }
        
        public TokenDto GetBySymbolCache(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return null;
            }

            return SymbolCache.TryGetValue(symbol, out var tokenDto) ? tokenDto : null;
        }

        public async Task<TokenDto> GetAsync(GetTokenInput input)
        {
            var tokenStateGrain =  _clusterClient.GetGrain<ITokenInfoGrain>(GrainIdHelper.GenerateGrainId(input.ChainId, input.Symbol));
            var token = await tokenStateGrain.GetAsync();
            if (token != null)
            {
                if (token.Success && !token.Data.IsEmpty())
                {
                    return _objectMapper.Map<TokenGrainDto, TokenDto>(token.Data);
                }
            }
            
            var mustQuery = new List<Func<QueryContainerDescriptor<TokenEntity>, QueryContainer>>();
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
            
            QueryContainer Filter(QueryContainerDescriptor<TokenEntity> f) => f.Bool(b => b.Must(mustQuery));
            var list = await _tokenIndexRepository.GetListAsync(Filter);
            var items = _objectMapper.Map<List<TokenEntity>, List<TokenDto>>(list.Item2);
            if (items.Count > 0)
            {
                return items[0];
            }
            
            // create
            var tokenInfo =
                await _aelfClientProvider.GetTokenInfoAsync(input.ChainId, null, input.Symbol);
            if (tokenInfo == null)
            {
                _logger.Error("GetTokenInfo from aelf client is null:{token}", input.Symbol);
                return null;
            }
            
            return await CreateAsync(new TokenCreateDto
            {
                Symbol = tokenInfo.Symbol,
                Address = tokenInfo.Address,
                Decimals = tokenInfo.Decimals,
                ChainId = input.ChainId,
                ImageUri = tokenInfo.ImageUri,
            });
        }

        public async Task<TokenDto> CreateAsync(TokenCreateDto input)
        {
            var token = _objectMapper.Map<TokenCreateDto, Token>(input);
            input.Id = (input.Id == Guid.Empty) ? Guid.NewGuid() : input.Id;
            token.Id = input.Id;

            var tokenStateGrain = _clusterClient.GetGrain<ITokenInfoGrain>(GrainIdHelper.GenerateGrainId(input.ChainId, input.Symbol));
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

            _logger.Information("token created: Id:{id}, ChainId:{chainId}, Symbol:{symbol}, Decimal:{decimal}, ImageUri:{ImageUri}",
                token.Id,
                token.ChainId, token.Symbol, token.Decimals, token.ImageUri);
            
            return tokenDto;
        }
    }
}