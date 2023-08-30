using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Trade.Dtos;
using Nest;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.Trade
{
    [RemoteService(IsEnabled = false)]
    public class KLineAppService : ApplicationService, IKLineAppService
    {
        private readonly INESTRepository<Index.KLine, Guid> _kLineIndexRepository;

        public KLineAppService(INESTRepository<Index.KLine, Guid> kLineIndexRepository)
        {
            _kLineIndexRepository = kLineIndexRepository;
        }

        public async Task<ListResultDto<KLineDto>> GetListAsync(GetKLinesInput input)
        {
            var timestampMin = KLineHelper.GetKLineTimestamp(input.Period, input.TimestampMin);
            var timestampMax = KLineHelper.GetKLineTimestamp(input.Period, input.TimestampMax);
            var mustQuery = new List<Func<QueryContainerDescriptor<Index.KLine>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TradePairId).Value(input.TradePairId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Period).Value(input.Period)));
            mustQuery.Add(q => q.Range(i =>
                i.Field(f => f.Timestamp)
                    .GreaterThanOrEquals(timestampMin)));
            mustQuery.Add(q => q.Range(i =>
                i.Field(f => f.Timestamp)
                    .LessThanOrEquals(timestampMax)));

            QueryContainer Filter(QueryContainerDescriptor<Index.KLine> f) => f.Bool(b => b.Must(mustQuery));
            
            var list = await _kLineIndexRepository.GetListAsync(Filter, sortExp: k => k.Timestamp);

            if (list.Item1 == 0 || list.Item2.First().Timestamp != timestampMin)
            {
                var lastKLine = await _kLineIndexRepository.GetAsync(q =>
                        q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)) &&
                        q.Term(i => i.Field(f => f.TradePairId).Value(input.TradePairId)) &&
                        q.Term(i => i.Field(f => f.Period).Value(input.Period)) &&
                        q.Range(i => i.Field(f => f.Timestamp).LessThan(timestampMin)),
                    sortExp: s => s.Timestamp, sortType: SortOrder.Descending);
                if (lastKLine != null)
                {
                    list.Item2.Insert(0,new Index.KLine
                    {
                        Close = lastKLine.Close,
                        High = lastKLine.Close,
                        Low = lastKLine.Close,
                        Open = lastKLine.Close,
                        Volume = 0,
                        Period = lastKLine.Period,
                        Timestamp = timestampMin,
                        ChainId = lastKLine.ChainId,
                        TradePairId = lastKLine.TradePairId
                    });
                }
            }

            if (list.Item2.Count != 0 && list.Item2.Last().Timestamp != timestampMax)
            {
                var lastItem = list.Item2.Last();
                list.Item2.Add(new Index.KLine
                {
                    Close = lastItem.Close,
                    High = lastItem.Close,
                    Low = lastItem.Close,
                    Open = lastItem.Close,
                    Volume = 0,
                    Period = lastItem.Period,
                    Timestamp = timestampMax,
                    ChainId = lastItem.ChainId,
                    TradePairId = lastItem.TradePairId
                });
            }

            return new ListResultDto<KLineDto>
            {
                Items = ObjectMapper.Map<List<Index.KLine>, List<KLineDto>>(list.Item2)
            };
        }
    }
}