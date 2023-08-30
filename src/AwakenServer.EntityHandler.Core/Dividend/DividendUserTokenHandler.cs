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
    public class DividendUserTokenHandler : IDistributedEventHandler<EntityCreatedEto<DividendUserTokenEto>>,
        IDistributedEventHandler<EntityUpdatedEto<DividendUserTokenEto>>, ITransientDependency
    {
        private readonly INESTRepository<DividendUserToken, Guid> _dividendUserTokenRepository;
        private readonly ILogger<DividendUserTokenHandler> _logger;

        public DividendUserTokenHandler(INESTRepository<DividendUserToken, Guid> dividendUserTokenRepository,
            ILogger<DividendUserTokenHandler> logger)
        {
            _dividendUserTokenRepository = dividendUserTokenRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityCreatedEto<DividendUserTokenEto> eventData)
        {
            var dividendUserTokenEto = eventData.Entity;
            _logger.LogInformation(
                "New dividend user token information is added");
            LogDividendUserTokenInformation(dividendUserTokenEto);
            await _dividendUserTokenRepository.AddAsync(dividendUserTokenEto);
        }

        public async Task HandleEventAsync(EntityUpdatedEto<DividendUserTokenEto> eventData)
        {
            var dividendUserTokenEto = eventData.Entity;
            _logger.LogInformation(
                "Dividend user token information is updated");
            LogDividendUserTokenInformation(dividendUserTokenEto);
            await _dividendUserTokenRepository.UpdateAsync(dividendUserTokenEto);
        }

        private void LogDividendUserTokenInformation(DividendUserTokenEto dividendTokenEto)
        {
            _logger.LogInformation(
                $"User: {dividendTokenEto.User}, token: {dividendTokenEto.DividendToken.Symbol} token accumulative amount: {dividendTokenEto.AccumulativeDividend}");
        }
    }
}