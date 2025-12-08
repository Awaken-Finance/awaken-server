using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AutoMapper.Internal.Mappers;
using AwakenServer.EntityHandler.Trade;
using AwakenServer.StatInfo.Etos;
using AwakenServer.StatInfo.Index;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using MassTransit;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.StatInfo
{
    public class StatInfoSnapshotIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<StatInfoSnapshotIndexEto>
    {
        private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
        private readonly ILogger<StatInfoSnapshotIndexHandler> _logger;
        private readonly IBus _bus;
        
        public StatInfoSnapshotIndexHandler(INESTRepository<StatInfoSnapshotIndex, Guid> statInfoSnapshotIndexRepository,
            IBus bus,
            ILogger<StatInfoSnapshotIndexHandler> logger) 
        {
            _statInfoSnapshotIndexRepository = statInfoSnapshotIndexRepository;
            _logger = logger;
            _bus = bus;
        }

        public async Task HandleEventAsync(StatInfoSnapshotIndexEto eventData)
        {
            await AddOrUpdateIndexAsync(eventData);
        }

        private async Task AddOrUpdateIndexAsync(StatInfoSnapshotIndexEto eto)
        {
            var statInfoSnapshotIndex = ObjectMapper.Map<StatInfoSnapshotIndexEto, StatInfoSnapshotIndex>(eto);
            await _statInfoSnapshotIndexRepository.AddOrUpdateAsync(statInfoSnapshotIndex);
        }
    }
}