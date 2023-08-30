using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Entities.GameOfTrust.Es;
using AwakenServer.ETOs.GameOfTrust;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.EntityHandler.GameOfTrust
{
    public class GameOfTrustRecordHandler:IDistributedEventHandler<EntityCreatedEto<GameOfTrustRecordCreatedEto>>,ITransientDependency
    {
        private readonly INESTRepository<GameOfTrustRecord, Guid> _recordRepository;
        private readonly IAutoObjectMappingProvider _mapper;


        public GameOfTrustRecordHandler(INESTRepository<GameOfTrustRecord, Guid> recordRepository, IAutoObjectMappingProvider mapper)
        {
            _recordRepository = recordRepository;
            _mapper = mapper;
        }

        public async Task HandleEventAsync(EntityCreatedEto<GameOfTrustRecordCreatedEto> eventData)
        {
            var gameOfTrustRecord = eventData.Entity;
            var gameOfTrustRecordEs = _mapper.Map<GameOfTrustRecordCreatedEto, GameOfTrustRecord>(gameOfTrustRecord);
            // todo 
            gameOfTrustRecordEs.ChainId = gameOfTrustRecord.GameOfTrust.DepositToken.ChainId;
            // todo 
            gameOfTrustRecordEs.GameOfTrust.ChainId = gameOfTrustRecord.GameOfTrust.DepositToken.ChainId;
            await _recordRepository.AddAsync(gameOfTrustRecordEs);
        }
    }
}