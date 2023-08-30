using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Etos;
using AwakenServer.Price.Index;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Price
{
    public class OtherLpTokenIndexHandler : PriceIndexHandlerBase, IDistributedEventHandler<EntityCreatedEto<OtherLpTokenEto>>,
        IDistributedEventHandler<EntityUpdatedEto<OtherLpTokenEto>>
    {
        private readonly INESTWriterRepository<OtherLpToken, Guid> _otherLpTokenIndexWriterRepository;

        public OtherLpTokenIndexHandler(INESTWriterRepository<OtherLpToken, Guid> otherLpTokenIndexWriterRepository)
        {
            _otherLpTokenIndexWriterRepository = otherLpTokenIndexWriterRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEto<OtherLpTokenEto> eventData)
        {
            var index = await MapEtoToOtherLpTokenAsync(eventData.Entity);
            await _otherLpTokenIndexWriterRepository.AddAsync(index);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<OtherLpTokenEto> eventData)
        {
            var index = await MapEtoToOtherLpTokenAsync(eventData.Entity);
            await _otherLpTokenIndexWriterRepository.UpdateAsync(index);
        }

        private async Task<OtherLpToken> MapEtoToOtherLpTokenAsync(OtherLpTokenEto eto)
        {
            var index = ObjectMapper.Map<OtherLpTokenEto, OtherLpToken>(eto);
            index.Token0 = await GetTokenAsync(eto.Token0Id);
            index.Token1 = await GetTokenAsync(eto.Token1Id);
            return index;
        }
    }
}