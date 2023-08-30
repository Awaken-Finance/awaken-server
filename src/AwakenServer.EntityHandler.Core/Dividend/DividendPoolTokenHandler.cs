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
    public class DividendPoolTokenHandler : IDistributedEventHandler<EntityCreatedEto<DividendPoolTokenEto>>,
        IDistributedEventHandler<EntityUpdatedEto<DividendPoolTokenEto>>,
        ITransientDependency
    {
        private readonly INESTRepository<DividendPoolToken, Guid> _dividendPoolTokenRepository;
        private readonly ILogger<DividendPoolTokenHandler> _logger;

        public DividendPoolTokenHandler(INESTRepository<DividendPoolToken, Guid> dividendPoolTokenRepository,
            ILogger<DividendPoolTokenHandler> logger)
        {
            _dividendPoolTokenRepository = dividendPoolTokenRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityCreatedEto<DividendPoolTokenEto> eventData)
        {
            var dividendPoolToken = eventData.Entity;
            _logger.LogInformation(
                "New pool dividend information is added");
            LogDividendPoolTokenInformation(dividendPoolToken);
            await _dividendPoolTokenRepository.AddAsync(dividendPoolToken);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<DividendPoolTokenEto> eventData)
        {
            var dividendPoolToken = eventData.Entity;
            _logger.LogInformation(
                "Pool dividend information is updated");
            LogDividendPoolTokenInformation(dividendPoolToken);
            await _dividendPoolTokenRepository.UpdateAsync(dividendPoolToken);
        }

        private void LogDividendPoolTokenInformation(DividendPoolTokenEto dividendPoolTokenEto)
        {
            _logger.LogInformation(
                $"Pid: {dividendPoolTokenEto.PoolBaseInfo.Pid} token symbol: {dividendPoolTokenEto.DividendToken.Symbol}, accumulative amount: {dividendPoolTokenEto.AccumulativeDividend}");
        }
    }
}