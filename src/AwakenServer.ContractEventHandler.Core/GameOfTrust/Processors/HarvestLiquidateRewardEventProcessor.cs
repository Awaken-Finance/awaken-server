using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Entities.GameOfTrust.Ef;
using AwakenServer.GameOfTrust;
using AwakenServer.Tokens;
using Nethereum.Util;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Processors
{
    public class
        HarvestLiquidateRewardEventProcessor : EthereumEthereumEventProcessorBase<HarvestLiquidateRewardEventDto>
    {
        private readonly IRepository<UserGameOfTrust> userRepository;
        private readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> gameRepository;
        private readonly IRepository<GameOfTrustRecord> recordRepository;
        private readonly ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> _gameInfoProvider;
        private readonly IChainAppService _chainAppService;
        private readonly ITokenProvider _tokenProvider;

        public HarvestLiquidateRewardEventProcessor(IRepository<UserGameOfTrust> userRepository,
            IRepository<Entities.GameOfTrust.Ef.GameOfTrust> gameRepository,
            IRepository<GameOfTrustRecord> recordRepository,
            ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> gameInfoProvider,
            IChainAppService chainAppService,
            ITokenProvider tokenProvider)
        {
            this.userRepository = userRepository;
            this.gameRepository = gameRepository;
            this.recordRepository = recordRepository;
            _gameInfoProvider = gameInfoProvider;
            _chainAppService = chainAppService;
            _tokenProvider = tokenProvider;
        }


        protected override async Task HandleEventAsync(HarvestLiquidateRewardEventDto eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            if (eventDetailsEto.Amount == 0)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var gameOfTrustCached = await _gameInfoProvider.GetOrSetCachedDataAsync(
                nodeName + contractEventDetailsDto.Address,
                x => x.ChainId == chain.Id
                     && x.Address == contractEventDetailsDto.Address
                     && x.Pid == eventDetailsEto.Pid);

            var gameOfTrust = await gameRepository.GetAsync(x => x.ChainId == chain.Id
                                                                 && x.Address == gameOfTrustCached.Address
                                                                 && x.Pid == eventDetailsEto.Pid);
            var harvestToken = _tokenProvider.GetToken(gameOfTrust.HarvestTokenId);
            var userInfo = await userRepository.GetAsync(x => x.GameOfTrustId == gameOfTrust.Id
                                                              && x.ChainId == chain.Id
                                                              && x.Address == eventDetailsEto.Receiver);
            var amout = BigDecimal.Parse(eventDetailsEto.Amount.ToString()) /
                               BigInteger.Pow(10, harvestToken.Decimals);

            userInfo.ReceivedFineAmount = amout.ToString();
            await userRepository.UpdateAsync(userInfo);
            gameOfTrust.FineAmount = (BigDecimal.Parse(gameOfTrust.FineAmount) - amout).ToString();
            await gameRepository.UpdateAsync(gameOfTrust);
            await recordRepository.InsertAsync(new GameOfTrustRecord
            {
                Address = eventDetailsEto.Receiver,
                Amount = amout.ToString(),
                Timestamp = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp*1000),
                Type = BehaviorType.Reward,
                ChainId = chain.Id,
                TransactionHash = contractEventDetailsDto.TransactionHash,
                GameOfTrustId = gameOfTrust.Id
            });
        }
    }
}