using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Dividend.Entities.Es;
using AwakenServer.Dividend.ETOs;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events.Distributed;
using Volo.Abp.EventBus.Distributed;

namespace AwakenServer.EntityHandler.Dividend
{
    public class DividendPoolHandler : IDistributedEventHandler<EntityCreatedEto<DividendPoolEto>>,
        IDistributedEventHandler<EntityUpdatedEto<DividendPoolEto>>, ITransientDependency
    {
        private readonly INESTRepository<DividendPool, Guid> _dividendPoolRepository;
        private readonly ILogger<DividendPoolHandler> _logger;

        public DividendPoolHandler(INESTRepository<DividendPool, Guid> dividendPoolRepository,
            ILogger<DividendPoolHandler> logger)
        {
            _dividendPoolRepository = dividendPoolRepository;
            _logger = logger;
        }
        
        public async Task HandleEventAsync(EntityCreatedEto<DividendPoolEto> eventData)
        {
            var dividendPoolEto = eventData.Entity;
            _logger.LogInformation("New dividend Pool is added");
            LogDividendPoolInformation(dividendPoolEto);
            await _dividendPoolRepository.AddAsync(dividendPoolEto);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<DividendPoolEto> eventData)
        {
            var dividendPoolEto = eventData.Entity;
            _logger.LogInformation("Dividend Pool is updated");
            LogDividendPoolInformation(dividendPoolEto);
            await _dividendPoolRepository.UpdateAsync(dividendPoolEto);
        }

        private void LogDividendPoolInformation(DividendPoolEto dividendPoolEto)
        {
            _logger.LogInformation($"Pid: {dividendPoolEto.Pid}");
        }
    }
}