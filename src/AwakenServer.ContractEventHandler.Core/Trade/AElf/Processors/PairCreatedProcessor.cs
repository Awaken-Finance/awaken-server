using System.Threading.Tasks;
using AElf;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Swap;
using AwakenServer.Chains;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Options;

namespace AwakenServer.ContractEventHandler.Trade.AElf.Processors
{
    public class PairCreatedProcessor : AElfEventProcessorBase<PairCreated>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITradePairAppService _tradePairAppService;
        private readonly FactoryContractOptions _factoryContractOptions;
        private readonly ITokenProvider _tokenProvider;
        private readonly ITradePairTokenOrderProvider _tradePairTokenOrderProvider;

        public PairCreatedProcessor(ITokenProvider tokenProvider, IChainAppService chainAppService,
            ITradePairAppService tradePairAppService, IOptionsSnapshot<FactoryContractOptions> pairFeeRateOptions, 
            ITradePairTokenOrderProvider tradePairTokenOrderProvider)
        {
            _tokenProvider = tokenProvider;
            _chainAppService = chainAppService;
            _tradePairAppService = tradePairAppService;
            _tradePairTokenOrderProvider = tradePairTokenOrderProvider;
            _factoryContractOptions = pairFeeRateOptions.Value;
        }

        protected override async Task HandleEventAsync(PairCreated eventDetailsEto, EventContext txInfoDto)
        {
            var nodeName = ChainHelper.ConvertChainIdToBase58(txInfoDto.ChainId);
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var token0 = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null, eventDetailsEto.SymbolA);
            var token1 = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null, eventDetailsEto.SymbolB);

            var token0Weight = _tradePairTokenOrderProvider.GetTokenWeight(token0.Address, token0.Symbol);
            var token1Weight = _tradePairTokenOrderProvider.GetTokenWeight(token1.Address, token1.Symbol);

            var isTokenReversed = token0Weight > token1Weight;

            await _tradePairAppService.CreateAsync(new TradePairCreateDto
            {
                ChainId = chain.Id,
                Address = eventDetailsEto.Pair.ToBase58(),
                Token0Id = isTokenReversed? token1.Id: token0.Id,
                Token1Id = isTokenReversed? token0.Id: token1.Id,
                FeeRate =
                    _factoryContractOptions.Contracts[txInfoDto.ToAddress],
                IsTokenReversed = isTokenReversed
            });
        }
    }
}