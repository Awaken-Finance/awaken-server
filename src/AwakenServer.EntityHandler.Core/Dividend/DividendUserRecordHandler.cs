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
    public class DividendUserRecordHandler : IDistributedEventHandler<EntityCreatedEto<DividendUserRecordEto>>,
        ITransientDependency
    {
        private readonly INESTRepository<DividendUserRecord, Guid> _dividendUserRecordRepository;
        private readonly ILogger<DividendUserRecordHandler> _logger;

        public DividendUserRecordHandler(INESTRepository<DividendUserRecord, Guid> dividendUserRecordRepository,
            ILogger<DividendUserRecordHandler> logger)
        {
            _dividendUserRecordRepository = dividendUserRecordRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityCreatedEto<DividendUserRecordEto> eventData)
        {
            var dividendUserRecordEto = eventData.Entity;
            _logger.LogInformation(
                "New dividend user record is added");
            LogDividendUserRecordInformation(dividendUserRecordEto);
            await _dividendUserRecordRepository.AddAsync(dividendUserRecordEto);
        }

        private void LogDividendUserRecordInformation(DividendUserRecordEto dividendTokenEto)
        {
            _logger.LogInformation(
                $"User: {dividendTokenEto.User}, operation type: {dividendTokenEto.BehaviorType}, token amount: {dividendTokenEto.Amount}");
        }
    }
}