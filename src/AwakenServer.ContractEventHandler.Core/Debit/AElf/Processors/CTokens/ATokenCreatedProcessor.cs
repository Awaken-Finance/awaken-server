using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.AToken;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Debits.Entities.Ef;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Debit.AElf.Processors.CTokens
{
    public class ATokenCreatedProcessor : AElfEventProcessorBase<TokenCreated>
    {
        private readonly IRepository<CompController> _compControllerRepository;
        private readonly IRepository<CToken> _cTokenRepository;
        private readonly  IChainAppService _chainAppService;
        private readonly ITokenProvider _tokenProvider;
        private const string ZeroBalance = "0";
        private readonly ILogger<ATokenCreatedProcessor> _logger;

        public ATokenCreatedProcessor(IChainAppService chainAppService,
            IRepository<CompController> compControllerRepository, IRepository<CToken> cTokenRepository,
            ITokenProvider tokenProvider, ILogger<ATokenCreatedProcessor> logger)
        {
            _chainAppService = chainAppService;
            _compControllerRepository = compControllerRepository;
            _cTokenRepository = cTokenRepository;
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        protected override async Task HandleEventAsync(TokenCreated eventDetailsEto, EventContext txInfoDto)
        {
            _logger.LogInformation($"TokenCreated Trigger: {eventDetailsEto}");
            var chainId = txInfoDto.ChainId;
            var chain = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            var controller =
                await _compControllerRepository.GetAsync(x =>
                    x.ChainId == chain.Id && x.ControllerAddress == eventDetailsEto.Controller.ToBase58());
            var targetCToken = await _cTokenRepository.FindAsync(x =>
                x.CompControllerId == controller.Id && x.Address == eventDetailsEto.AToken.ToBase58());
            if (targetCToken != null)
            {
                return;
            }

            var underlyingToken =
                await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null,
                    eventDetailsEto.Underlying);
            await _cTokenRepository.InsertAsync(new CToken
            {
                ChainId = chain.Id,
                CompControllerId = controller.Id,
                Address = eventDetailsEto.AToken.ToBase58(),
                Symbol = eventDetailsEto.Symbol,
                Decimals = eventDetailsEto.Decimals,
                UnderlyingTokenId = underlyingToken.Id,
                TotalCTokenMintAmount = ZeroBalance,
                TotalUnderlyingAssetBorrowAmount = ZeroBalance,
                TotalUnderlyingAssetReserveAmount = ZeroBalance,
                TotalUnderlyingAssetAmount = ZeroBalance,
                IsBorrowPaused = false,
                IsMintPaused = false,
                IsList = false,
                BorrowCompSpeed = ZeroBalance,
                SupplyCompSpeed = ZeroBalance,
                AccumulativeBorrowComp = ZeroBalance,
                AccumulativeSupplyComp = ZeroBalance,
                CollateralFactorMantissa = ZeroBalance,
                ReserveFactorMantissa = ZeroBalance
            });
        }
    }
}