using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class NewRewardProcessor : AElfEventProcessorBase<NewReward>
    {
        private readonly IRepository<AwakenServer.Dividend.Entities.Dividend> _dividendRepository;
        private readonly IRepository<DividendToken> _dividendTokenRepository;
        private readonly IDividendCacheService _dividendCacheService;
        private readonly ITokenProvider _tokenProvider;
        private readonly string _processorName = "DividendNewRewardProcessor";

        public NewRewardProcessor(IRepository<AwakenServer.Dividend.Entities.Dividend> dividendRepository,
            IDividendCacheService dividendCacheService, IRepository<DividendToken> dividendTokenRepository,
            ITokenProvider tokenProvider)
        {
            _dividendRepository = dividendRepository;
            _dividendCacheService = dividendCacheService;
            _dividendTokenRepository = dividendTokenRepository;
            _tokenProvider = tokenProvider;
        }
        
        public override string GetProcessorName()
        {
            return _processorName;
        }

        protected override async Task HandleEventAsync(NewReward eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var dividendBaseInfo =
                await _dividendCacheService.GetDividendBaseInfoAsync(chain.Id, txInfoDto.EventAddress);
            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null, eventDetailsEto.Token);
            var dividendToken =
                await _dividendTokenRepository.GetAsync(x => x.DividendId == dividendBaseInfo.Id && x.TokenId == token.Id);
            dividendToken.AmountPerBlock = eventDetailsEto.PerBlocks.Value;
            dividendToken.StartBlock = eventDetailsEto.StartBlock;
            dividendToken.EndBlock = eventDetailsEto.EndBlock;
            await _dividendTokenRepository.UpdateAsync(dividendToken);
        }
    }
}