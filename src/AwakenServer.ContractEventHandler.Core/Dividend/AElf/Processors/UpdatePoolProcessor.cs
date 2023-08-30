using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class UpdatePoolProcessor : AElfEventProcessorBase<UpdatePool>
    {
        private readonly IDividendCacheService _dividendCacheService;
        private readonly IRepository<DividendPoolToken> _dividendPoolTokenRepository;
        private readonly ITokenProvider _tokenProvider;
        private readonly string _processorName = "DividendUpdatePoolProcessor";

        public UpdatePoolProcessor(IDividendCacheService dividendCacheService,
            IRepository<DividendPoolToken> dividendPoolTokenRepository,
            ITokenProvider tokenProvider)
        {
            _dividendCacheService = dividendCacheService;
            _dividendPoolTokenRepository = dividendPoolTokenRepository;
            _tokenProvider = tokenProvider;
        }
        
        public override string GetProcessorName()
        {
            return _processorName;
        }

        protected override async Task HandleEventAsync(UpdatePool eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var dividendBaseInfo =
                await _dividendCacheService.GetDividendBaseInfoAsync(chain.Id, txInfoDto.EventAddress);
            var dividendPool = await _dividendCacheService.GetDividendPoolBaseInfoAsync(dividendBaseInfo.Id,
                eventDetailsEto.Pid);
            var dividendToken =
                await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null, eventDetailsEto.Token);
            var poolToken =
                await _dividendPoolTokenRepository.FindAsync(x =>
                    x.PoolId == dividendPool.Id && x.DividendTokenId == dividendToken.Id);
            if (poolToken == null)
            {
                await _dividendPoolTokenRepository.InsertAsync(new DividendPoolToken
                {
                    PoolId = dividendPool.Id,
                    DividendTokenId = dividendToken.Id,
                    ChainId = chain.Id,
                    AccumulativeDividend = eventDetailsEto.Reward.Value,
                    LastRewardBlock = eventDetailsEto.BlockHeigh
                });
                return;
            }

            poolToken.LastRewardBlock = eventDetailsEto.BlockHeigh;
            poolToken.AccumulativeDividend =
                CalculationHelper.Add(poolToken.AccumulativeDividend, eventDetailsEto.Reward.Value);
            await _dividendPoolTokenRepository.UpdateAsync(poolToken);
        }
    }
}