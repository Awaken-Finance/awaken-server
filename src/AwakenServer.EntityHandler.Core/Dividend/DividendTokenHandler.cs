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
    public class DividendTokenHandler : IDistributedEventHandler<EntityCreatedEto<DividendTokenEto>>,
        IDistributedEventHandler<EntityUpdatedEto<DividendTokenEto>>
        , ITransientDependency
    {
        private readonly INESTRepository<DividendToken, Guid> _dividendTokenRepository;
        private readonly ILogger<DividendTokenHandler> _logger;

        public DividendTokenHandler(INESTRepository<DividendToken, Guid> dividendTokenRepository,
            ILogger<DividendTokenHandler> logger)
        {
            _dividendTokenRepository = dividendTokenRepository;
            _logger = logger;
        }

        public async Task HandleEventAsync(EntityCreatedEto<DividendTokenEto> eventData)
        {
            var dividendToken = eventData.Entity;
            dividendToken.AmountPerBlock = "0";
            _logger.LogInformation(
                "New dividend token is added");
            LogDividendTokenInformation(dividendToken);
            await _dividendTokenRepository.AddAsync(dividendToken);
        }
        
        public async Task HandleEventAsync(EntityUpdatedEto<DividendTokenEto> eventData)
        {
            var dividendToken = eventData.Entity;
            _logger.LogInformation(
                "Dividend token information is updated");
            LogDividendTokenInformation(dividendToken);
            await _dividendTokenRepository.UpdateAsync(dividendToken);
        }

        private void LogDividendTokenInformation(DividendTokenEto dividendTokenEto)
        {
            _logger.LogInformation(
                $"Token symbol: {dividendTokenEto.Token.Symbol}");
        }
    }
}