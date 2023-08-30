using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using Awaken.Contracts.Shadowfax;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.IDO.AElf.Processors
{
    public class AddPublicOfferingProcessor : AElfEventProcessorBase<AddPublicOffering>
    {
        private readonly IRepository<AwakenServer.IDO.Entities.Ef.PublicOffering> _publicOfferingRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ITokenProvider _tokenProvider;
        private readonly ILogger<AddPublicOfferingProcessor> _logger;

        public AddPublicOfferingProcessor(IRepository<AwakenServer.IDO.Entities.Ef.PublicOffering> publicOfferingRepository,
            ILogger<AddPublicOfferingProcessor> logger, IChainAppService chainAppService,
            ITokenProvider tokenProvider)
        {
            _publicOfferingRepository = publicOfferingRepository;
            _logger = logger;
            _chainAppService = chainAppService;
            _tokenProvider = tokenProvider;
        }

        protected override async Task HandleEventAsync(AddPublicOffering eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _chainAppService.GetByChainIdCacheAsync(txInfoDto.ChainId.ToString());
            var isExisted =
                await _publicOfferingRepository.FindAsync(p =>
                    p.OrderRank == eventDetailsEto.PublicId && p.ChainId == chain.Id) != null;
            if (isExisted)
            {
                _logger.LogInformation($"Public Id has been existed: {eventDetailsEto.PublicId}");
                _logger.LogInformation(eventDetailsEto.ToString());
                return;
            }

            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, string.Empty,
                eventDetailsEto.OfferingTokenSymbol);
            var raiseToken = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, string.Empty,
                eventDetailsEto.WantTokenSymbol);
            var publicOffering = new AwakenServer.IDO.Entities.Ef.PublicOffering
            {
                ChainId = chain.Id,
                OrderRank = eventDetailsEto.PublicId,
                TokenContractAddress = string.Empty, // todo may need to be compatible with lp token
                MaxAmount = eventDetailsEto.OfferingTokenAmount,
                RaiseMaxAmount = eventDetailsEto.WantTokenAmount,
                CurrentAmount = eventDetailsEto.OfferingTokenAmount,
                RaiseCurrentAmount = 0,
                Publisher = eventDetailsEto.Publisher.ToBase58(),
                StartTime = eventDetailsEto.StartTime.ToDateTime(),
                EndTime = eventDetailsEto.EndTime.ToDateTime(),
                TokenId = token.Id,
                RaiseTokenId = raiseToken.Id
            };
            await _publicOfferingRepository.InsertAsync(publicOffering);
        }
    }
}