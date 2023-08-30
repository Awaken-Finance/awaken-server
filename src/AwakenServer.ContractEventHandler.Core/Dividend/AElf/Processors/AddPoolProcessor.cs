using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class AddPoolProcessor : AElfEventProcessorBase<AddPool>
    {
        private readonly IRepository<AwakenServer.Dividend.Entities.Dividend> _dividendRepository;
        private readonly IRepository<DividendPool> _dividendPoolRepository;
        private readonly IDividendCacheService _dividendCacheService;
        private readonly ITokenProvider _tokenProvider;

        public AddPoolProcessor(IRepository<AwakenServer.Dividend.Entities.Dividend> dividendRepository,
            IRepository<DividendPool> dividendPoolRepository,
            IDividendCacheService dividendCacheService, ITokenProvider tokenProvider)
        {
            _dividendRepository = dividendRepository;
            _dividendPoolRepository = dividendPoolRepository;
            _dividendCacheService = dividendCacheService;
            _tokenProvider = tokenProvider;
        }

        protected override async Task HandleEventAsync(AddPool eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null, eventDetailsEto.Token);
            var dividend =
                await _dividendRepository.GetAsync(x => x.ChainId == chain.Id && x.Address == txInfoDto.EventAddress);
            var pool = await _dividendPoolRepository.FindAsync(x =>
                x.DividendId == dividend.Id && x.Pid == eventDetailsEto.Pid);
            if (pool != null)
            {
                return;
            }

            var weight = (int)eventDetailsEto.AllocPoint;
            pool = new DividendPool
            {
                ChainId = chain.Id,
                DividendId = dividend.Id,
                PoolTokenId = token.Id,
                Pid = eventDetailsEto.Pid,
                DepositAmount = "0",
                Weight = weight
            };
            await _dividendPoolRepository.InsertAsync(pool, true);
            dividend.TotalWeight += weight;
            await _dividendRepository.UpdateAsync(dividend);
        }
    }
}