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

public class TransactionHistoryIndexHandler : TradeIndexHandlerBase, IDistributedEventHandler<TransactionHistoryEto>
{
    private readonly INESTRepository<TransactionHistoryIndex, Guid> _repository;
    private readonly ILogger<TransactionHistoryIndexHandler> _logger;
    private readonly IBus _bus;

    public TransactionHistoryIndexHandler(INESTRepository<TransactionHistoryIndex, Guid> repository, ILogger<TransactionHistoryIndexHandler> logger, IBus bus)
    {
        _repository = repository;
        _logger = logger;
        _bus = bus;
    }

    public async Task HandleEventAsync(TransactionHistoryEto eventData)
    {
        var transactionHistoryIndex = ObjectMapper.Map<TransactionHistoryEto, TransactionHistoryIndex>(eventData);
        var existedIndex = await _repository.GetAsync(q =>
            q.Term(i => i.Field(f => f.Version).Value(eventData.Version)) &&
            q.Term(i => i.Field(f => f.TransactionHash).Value(eventData.TransactionHash)));
        transactionHistoryIndex.Id = existedIndex switch
        {
            null => Guid.NewGuid(),
            _ => existedIndex.Id
        };
        await _repository.AddOrUpdateAsync(transactionHistoryIndex);
    }
}