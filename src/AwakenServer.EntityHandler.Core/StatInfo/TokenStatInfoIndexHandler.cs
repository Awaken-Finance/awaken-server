using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.EntityHandler.Trade;
using AwakenServer.StatInfo.Etos;
using AwakenServer.StatInfo.Index;
using MassTransit;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.StatInfo;

public class TokenStatInfoIndexHandler : TradeIndexHandlerBase, IDistributedEventHandler<TokenStatInfoEto>
{
    private readonly INESTRepository<TokenStatInfoIndex, Guid> _repository;
    private readonly ILogger<TokenStatInfoIndexHandler> _logger;
    private readonly IBus _bus;

    public TokenStatInfoIndexHandler(INESTRepository<TokenStatInfoIndex, Guid> repository, ILogger<TokenStatInfoIndexHandler> logger, IBus bus)
    {
        _repository = repository;
        _logger = logger;
        _bus = bus;
    }

    public async Task HandleEventAsync(TokenStatInfoEto eventData)
    {
        var tokenStatInfoIndex = ObjectMapper.Map<TokenStatInfoEto, TokenStatInfoIndex>(eventData);
        // var existedIndex = await _repository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.Version).Value(eventData.Version)) &&
        //     q.Term(i => i.Field(f => f.Symbol).Value(eventData.Symbol)));
        // tokenStatInfoIndex.Id = existedIndex switch
        // {
        //     null => Guid.NewGuid(),
        //     _ => existedIndex.Id
        // };
        await _repository.AddOrUpdateAsync(tokenStatInfoIndex);
    }
}