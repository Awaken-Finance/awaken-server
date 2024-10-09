using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using MassTransit;
using Microsoft.Extensions.Logging;
using Serilog;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Trade
{
    public class KLineIndexHandler : TradeIndexHandlerBase,
        IDistributedEventHandler<KLineEto>
    {
        private readonly INESTRepository<KLine, Guid> _kLineIndexRepository;
        private readonly ILogger<KLineIndexHandler> _logger;
        private readonly IBus _bus;
        public KLineIndexHandler(INESTRepository<KLine, Guid> kLineIndexRepository,
            IBus bus,
            ILogger<KLineIndexHandler> logger) 
        {
            _kLineIndexRepository = kLineIndexRepository;
            _logger = logger;
            _bus = bus;
        }

        public async Task HandleEventAsync(KLineEto eventData)
        {
            await AddOrUpdateIndexAsync(eventData);
        }

        private async Task AddOrUpdateIndexAsync(KLineEto eto)
        {
            var existIndex = await _kLineIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(eto.ChainId)) &&
                q.Term(i => i.Field(f => f.TradePairId).Value(eto.TradePairId)) &&
                q.Term(i => i.Field(f => f.Period).Value(eto.Period)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(eto.Timestamp)));

            if (existIndex == null)
            {
                existIndex = new KLine(Guid.NewGuid())
                {
                    ChainId = eto.ChainId,
                    TradePairId = eto.TradePairId,
                    Period = eto.Period,
                    Timestamp = eto.Timestamp
                };
            }

            existIndex.Open = eto.Open;
            existIndex.Close = eto.Close;
            existIndex.High = eto.High;
            existIndex.Low = eto.Low;
            existIndex.OpenWithoutFee = eto.OpenWithoutFee;
            existIndex.CloseWithoutFee = eto.CloseWithoutFee;
            existIndex.HighWithoutFee = eto.HighWithoutFee;
            existIndex.LowWithoutFee = eto.LowWithoutFee;
            existIndex.Volume = eto.Volume;

            await _kLineIndexRepository.AddOrUpdateAsync(existIndex);

            Log.Information("KLineIndexHandler: PublishAsync KLineDto:Period:{period},Timestamp:{timestamp}", eto.Period, eto.Timestamp);
            await _bus.Publish(new NewIndexEvent<KLineDto>
            {
                Data = ObjectMapper.Map<KLine, KLineDto>(existIndex)
            });
           
        }
    }
}