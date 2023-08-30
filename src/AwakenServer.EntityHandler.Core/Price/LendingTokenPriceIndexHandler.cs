using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Etos;
using AwakenServer.Price.Index;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Price
{
    public class LendingTokenPriceIndexHandler : PriceIndexHandlerBase, IDistributedEventHandler<EntityCreatedEto<LendingTokenPriceEto>>,
        IDistributedEventHandler<EntityUpdatedEto<LendingTokenPriceEto>>
    {
        private readonly INESTWriterRepository<LendingTokenPrice, Guid> _lendingTokenPriceIndexWriterRepository;
        private readonly INESTRepository<LendingTokenPriceHistory, Guid> _lendingTokenPriceHistoryIndexRepository;

        private const int SecondsPerDay = 86400;

        public LendingTokenPriceIndexHandler(INESTWriterRepository<LendingTokenPrice, Guid> lendingTokenPriceIndexWriterRepository, 
            INESTRepository<LendingTokenPriceHistory, Guid> lendingTokenPriceHistoryIndexRepository)
        {
            _lendingTokenPriceIndexWriterRepository = lendingTokenPriceIndexWriterRepository;
            _lendingTokenPriceHistoryIndexRepository = lendingTokenPriceHistoryIndexRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEto<LendingTokenPriceEto> eventData)
        {
            await AddPriceIndexAsync(eventData.Entity);
            await AddPriceHistoryIndexAsync(eventData.Entity);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<LendingTokenPriceEto> eventData)
        {
            await UpdatePriceIndexAsync(eventData.Entity);
            await AddOrUpdatePriceHistoryIndexAsync(eventData.Entity);
        }

        private async Task AddPriceIndexAsync(LendingTokenPriceEto eto)
        {
            var index = ObjectMapper.Map<LendingTokenPriceEto, LendingTokenPrice>(eto);
            index.Token = await GetTokenAsync(eto.TokenId);
            await _lendingTokenPriceIndexWriterRepository.AddAsync(index);
        }

        private async Task UpdatePriceIndexAsync(LendingTokenPriceEto eto)
        {
            var index = ObjectMapper.Map<LendingTokenPriceEto, LendingTokenPrice>(eto);
            index.Token = await GetTokenAsync(eto.TokenId);
            await _lendingTokenPriceIndexWriterRepository.UpdateAsync(index);
        }
        
        private async Task AddPriceHistoryIndexAsync(LendingTokenPriceEto eto)
        {
            var index = new LendingTokenPriceHistory(Guid.NewGuid());
            index = ObjectMapper.Map(eto, index);
            index.Token = await GetTokenAsync(eto.TokenId);
            index.Timestamp = eto.Timestamp.Date;
            index.UpdateTimestamp = eto.Timestamp;
            await _lendingTokenPriceHistoryIndexRepository.AddOrUpdateAsync(index);
        }
        
        private async Task AddOrUpdatePriceHistoryIndexAsync(LendingTokenPriceEto eto)
        {
            var index = await _lendingTokenPriceHistoryIndexRepository.GetAsync(q =>
                q.Term(i => i.Field(f => f.ChainId).Value(eto.ChainId)) &&
                q.Term(i => i.Field(f => f.Token.Id).Value(eto.TokenId)) &&
                q.Term(i => i.Field(f => f.Timestamp).Value(eto.Timestamp.Date)));
            //TODO There is a problem that create and update index in es search quickly.It will cause du
            if (index == null)
            {
                await AddPriceHistoryIndexAsync(eto);
            }
            else if (index.UpdateTimestamp <= eto.Timestamp && index.BlockNumber <= eto.BlockNumber)
            {
                index = ObjectMapper.Map(eto, index);
                index.UpdateTimestamp = eto.Timestamp;
                await _lendingTokenPriceHistoryIndexRepository.UpdateAsync(index);
            }
        }
    }
}