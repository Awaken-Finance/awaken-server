using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Dividend.ETOs;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Dividend
{
    public class DividendHandler : IDistributedEventHandler<EntityUpdatedEto<DividendEto>>, ITransientDependency
    {
        private readonly INESTRepository<AwakenServer.Dividend.Entities.Dividend, Guid> _dividendRepository;
        private readonly ILogger<DividendHandler> _logger;

        public DividendHandler(ILogger<DividendHandler> logger,
            INESTRepository<AwakenServer.Dividend.Entities.Dividend, Guid> dividendRepository)
        {
            _logger = logger;
            _dividendRepository = dividendRepository;
        }

        public async Task HandleEventAsync(EntityUpdatedEto<DividendEto> eventData)
        {
            var dividend = eventData.Entity;
            _logger.LogInformation("Dividend information is update");
            await _dividendRepository.AddOrUpdateAsync(dividend);
        }
    }
}