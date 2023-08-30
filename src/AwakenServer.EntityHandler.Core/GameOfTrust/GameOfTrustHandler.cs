using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.ETOs.GameOfTrust;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.EntityHandler.GameOfTrust
{
    public class GameOfTrustHandler : IDistributedEventHandler<EntityUpdatedEto<GameChangedEto>>,
        IDistributedEventHandler<EntityCreatedEto<GameChangedEto>>, ITransientDependency
    {
        private readonly INESTRepository<Entities.GameOfTrust.Es.GameOfTrust, Guid> _gameRepository;
        private readonly IAutoObjectMappingProvider _mapper;

        public GameOfTrustHandler(INESTRepository<Entities.GameOfTrust.Es.GameOfTrust, Guid> gameRepository, IAutoObjectMappingProvider mapper)
        {
            _gameRepository = gameRepository;
            _mapper = mapper;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<GameChangedEto> eventData)
        {
            var gameOfTrustCreated = eventData.Entity;
            var gameOfTrust= _mapper.Map<GameChangedEto,Entities.GameOfTrust.Es.GameOfTrust>(gameOfTrustCreated);
            // todo 
            gameOfTrust.ChainId = gameOfTrustCreated.DepositToken.ChainId;
            await _gameRepository.UpdateAsync(gameOfTrust);
        }   

        public async Task HandleEventAsync(EntityCreatedEto<GameChangedEto> eventData)
        {
            var gameOfTrustCreated = eventData.Entity;
            var gameOfTrust= _mapper.Map<GameChangedEto,Entities.GameOfTrust.Es.GameOfTrust>(gameOfTrustCreated);
            // todo 
            gameOfTrust.ChainId = gameOfTrustCreated.DepositToken.ChainId;
            await _gameRepository.AddAsync(gameOfTrust);
        }
    }
}