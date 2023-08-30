using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class AddTokenProcessor : AElfEventProcessorBase<AddToken>
    {
        private readonly IDividendCacheService _dividendCacheService;
        private readonly ITokenProvider _tokenProvider;
        private readonly IRepository<AwakenServer.Dividend.Entities.Dividend> _dividendRepository;
        private readonly IRepository<DividendToken> _dividendTokenRepository;

        public AddTokenProcessor(
            IRepository<AwakenServer.Dividend.Entities.Dividend> dividendRepository,
            IRepository<DividendToken> dividendTokenRepository,
            IDividendCacheService dividendCacheService, ITokenProvider tokenProvider)
        {
            _dividendRepository = dividendRepository;
            _dividendTokenRepository = dividendTokenRepository;
            _dividendCacheService = dividendCacheService;
            _tokenProvider = tokenProvider;
        }

        protected override async Task HandleEventAsync(AddToken eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null,
                eventDetailsEto.TokenSymbol);
            var dividend =
                await _dividendRepository.GetAsync(x => x.ChainId == chain.Id && x.Address == txInfoDto.EventAddress);
            var dividendToken =
                await _dividendTokenRepository.FindAsync(x => x.DividendId == dividend.Id && x.TokenId == token.Id);
            if (dividendToken != null)
            {
                return;
            }

            await _dividendTokenRepository.InsertAsync(new DividendToken
            {
                ChainId = chain.Id,
                DividendId = dividend.Id,
                TokenId = token.Id,
                AmountPerBlock = "0"
            }, true);
        }
    }
}