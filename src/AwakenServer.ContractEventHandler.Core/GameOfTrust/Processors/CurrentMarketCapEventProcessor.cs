using System;
using System.Data;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Entities.GameOfTrust.Ef;
using AwakenServer.Tokens;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Processors
{
    public class CurrentMarketCapEventProcessor : EthereumEthereumEventProcessorBase<CurrentMarketCapEventDto>
    {
        private readonly IRepository<GameOfTrustMarketData> _marketCapsRepository;
        private readonly IChainAppService _chainAppService;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly AnchorCoinsOptions _coinsOptions;
        private readonly ITokenAppService _tokenAppService;
        private readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> _repository;

        public CurrentMarketCapEventProcessor(
            IRepository<GameOfTrustMarketData> marketCapsRepository,
            IChainAppService chainAppService,
            IUnitOfWorkManager unitOfWorkManager,
            IOptionsSnapshot<AnchorCoinsOptions> coinsOptions, ITokenAppService tokenAppService,
            ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> gameInfoProvider,
            IRepository<Entities.GameOfTrust.Ef.GameOfTrust> repository)
        {
            _marketCapsRepository = marketCapsRepository;
            _chainAppService = chainAppService;
            _unitOfWorkManager = unitOfWorkManager;
            _tokenAppService = tokenAppService;
            _repository = repository;
            _coinsOptions = coinsOptions.Value;
        }

        protected override async Task HandleEventAsync(CurrentMarketCapEventDto eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            using var uow = _unitOfWorkManager.Begin(true, true, IsolationLevel.ReadCommitted);
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            AnchorCoin anchorCoin = null;
            foreach (var coin in _coinsOptions.AnchorCoinsList)
            {
                if (coin.Chain.Equals(chain.Name))
                {
                    anchorCoin = coin;
                }
            }

            if (anchorCoin == null)
            {
                throw new Exception("AnchorCoin config wrong.");
            }

            var gameOfTrust = await _repository.FirstOrDefaultAsync(trust =>
                trust.ChainId == chain.Id && trust.Address == contractEventDetailsDto.Address);
            var harvestToken = await _tokenAppService.GetAsync(gameOfTrust.HarvestTokenId);

            var currentMarketCap = await _marketCapsRepository.FirstOrDefaultAsync(
                x => x.ChainId == chain.Id);
            var averagePrice = BigDecimal.Parse(eventDetailsEto.AveragePrice.ToString()) /
                               BigInteger.Pow(10, anchorCoin.Decimal);
            var marketCap = BigDecimal.Parse(eventDetailsEto.MarketCap.ToString()) /
                            BigInteger.Pow(10, anchorCoin.Decimal);
            var totalSupply = BigDecimal.Parse(eventDetailsEto.TotalSupply.ToString()) /
                              BigInteger.Pow(10, harvestToken.Decimals);
            

            await _marketCapsRepository.InsertAsync(new GameOfTrustMarketData
            {
                Price = averagePrice.ToString(),
                ChainId = chain.Id,
                MarketCap = marketCap.ToString(),
                TotalSupply = totalSupply.ToString(),
                Timestamp = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp * 1000)
            });
            await uow.CompleteAsync();
        }
    }
}