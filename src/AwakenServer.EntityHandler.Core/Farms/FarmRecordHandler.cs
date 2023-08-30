using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.EntityHandler.Helpers;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Es;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Farms
{
    public class FarmRecordHandler : IDistributedEventHandler<EntityCreatedEto<FarmRecordChangedEto>>,
        ITransientDependency
    {
        private readonly INESTRepository<FarmRecord, Guid> _recordRepository;

        public FarmRecordHandler(INESTRepository<FarmRecord, Guid> recordRepository)
        {
            _recordRepository = recordRepository;
        }

        public async Task HandleEventAsync(EntityCreatedEto<FarmRecordChangedEto> eventData)
        {
            var record = eventData.Entity;
            record.DecimalAmount = CalculationHelper.GetDecimalAmount(record.Amount, record.TokenInfo.Decimals);
            await _recordRepository.AddAsync(record);
        }
    }
}