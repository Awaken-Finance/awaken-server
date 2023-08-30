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
    public class DividendUserPoolHandler : IDistributedEventHandler<EntityCreatedEto<DividendUserPoolEto>>,
        IDistributedEventHandler<EntityUpdatedEto<DividendUserPoolEto>>, ITransientDependency
    {
        private readonly INESTRepository<DividendUserPool, Guid> _dividendUserPoolRepository;
        private readonly ILogger<DividendUserPoolHandler> _logger;

        public DividendUserPoolHandler(INESTRepository<DividendUserPool, Guid> dividendUserPoolRepository,
            ILogger<DividendUserPoolHandler> logger)
        {
            _dividendUserPoolRepository = dividendUserPoolRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityCreatedEto<DividendUserPoolEto> eventData)
        {
            var dividendUserTokenEto = eventData.Entity;
            _logger.LogInformation(
                "User pool information is added");
            LogDividendUserPoolInformation(dividendUserTokenEto);
            await _dividendUserPoolRepository.AddAsync(dividendUserTokenEto);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<DividendUserPoolEto> eventData)
        {
            var dividendUserTokenEto = eventData.Entity;
            _logger.LogInformation(
                "User pool information is updated");
            LogDividendUserPoolInformation(dividendUserTokenEto);
            await _dividendUserPoolRepository.UpdateAsync(dividendUserTokenEto);
        }

        private void LogDividendUserPoolInformation(DividendUserPoolEto dividendUserPoolEto)
        {
            _logger.LogInformation(
                $"User: {dividendUserPoolEto.User}, pid: {dividendUserPoolEto.PoolBaseInfo.Pid} deposit: {dividendUserPoolEto.DepositAmount}");
        }
    }
}