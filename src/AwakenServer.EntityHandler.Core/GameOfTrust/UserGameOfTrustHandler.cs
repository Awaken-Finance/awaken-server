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
    public class UserGameOfTrustHandler:IDistributedEventHandler<EntityCreatedEto<UserGameOfTrustChangedEto>>,
        IDistributedEventHandler<EntityUpdatedEto<UserGameOfTrustChangedEto>>,ITransientDependency
    {

        private readonly INESTRepository<UserGameOfTrust, Guid> _userResRepository;
        private readonly IAutoObjectMappingProvider _mapper;

        public UserGameOfTrustHandler(INESTRepository<UserGameOfTrust, Guid> userResRepository, IAutoObjectMappingProvider mapper)
        {
            _userResRepository = userResRepository;
            _mapper = mapper;
        }

        public async Task HandleEventAsync(EntityCreatedEto<UserGameOfTrustChangedEto> eventData)
        {
           var userCreatedEto =  eventData.Entity;
           var userCreated = _mapper.Map<UserGameOfTrustChangedEto, UserGameOfTrust>(userCreatedEto);
           // todo 
           userCreated.ChainId = userCreatedEto.GameOfTrust.DepositToken.ChainId;
           userCreated.GameOfTrust.ChainId = userCreatedEto.GameOfTrust.DepositToken.ChainId;
           await _userResRepository.AddAsync(userCreated);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<UserGameOfTrustChangedEto> eventData)
        {
            var userChanged = eventData.Entity;
            var user = _mapper.Map<UserGameOfTrustChangedEto, UserGameOfTrust>(userChanged);
            // todo 
            user.ChainId = userChanged.GameOfTrust.DepositToken.ChainId;
            user.GameOfTrust.ChainId = userChanged.GameOfTrust.DepositToken.ChainId;
            await _userResRepository.UpdateAsync(user);
        }
    }
}