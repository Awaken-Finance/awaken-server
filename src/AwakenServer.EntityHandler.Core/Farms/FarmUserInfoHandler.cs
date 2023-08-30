using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.ETOs.Farms;
using AwakenServer.Farms.Entities.Es;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;
using Microsoft.Extensions.Logging;

namespace AwakenServer.EntityHandler.Farms
{
    public class FarmUserInfoHandler : IDistributedEventHandler<EntityUpdatedEto<FarmUserInfoChangedEto>>,
        IDistributedEventHandler<EntityCreatedEto<FarmUserInfoChangedEto>>,
        ITransientDependency
    {
        private readonly INESTRepository<FarmUserInfo, Guid> _userRepository;
        private readonly ILogger<FarmUserInfoHandler> _logger;

        public FarmUserInfoHandler(INESTRepository<FarmUserInfo, Guid> userRepository,
            ILogger<FarmUserInfoHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<FarmUserInfoChangedEto> eventData)
        {
            var esUserInfo = eventData.Entity;
            await _userRepository.UpdateAsync(esUserInfo);
        }

        public async Task HandleEventAsync(EntityCreatedEto<FarmUserInfoChangedEto> eventData)
        {
            var farmUserInfo = eventData.Entity;
            await _userRepository.AddAsync(farmUserInfo);
        }
    }
}