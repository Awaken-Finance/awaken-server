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
    public class HarvestEventProcessor : EthereumEthereumEventProcessorBase<HarvestEVentDto>
    {
        private readonly IRepository<UserGameOfTrust> _userRepository;
        private readonly IRepository<GameOfTrustRecord> _recordRepository;
        private readonly ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> _gameInfoProvider;
        private readonly IChainAppService _chainAppService;
        private readonly ITokenProvider _tokenProvider;

        public HarvestEventProcessor(IRepository<UserGameOfTrust> userRepository,
            IRepository<GameOfTrustRecord> recordRepository,
            ICachedDataProvider<Entities.GameOfTrust.Ef.GameOfTrust> gameInfoProvider,
            IChainAppService chainAppService,
            ITokenProvider tokenProvider)
        {
            _userRepository = userRepository;
            _recordRepository = recordRepository;
            _gameInfoProvider = gameInfoProvider;
            _chainAppService = chainAppService;
            _tokenProvider = tokenProvider;
        }


        protected override async Task HandleEventAsync(HarvestEVentDto eventDetailsEto,
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
            var gameOfTrust =
                await _gameInfoProvider.GetOrSetCachedDataAsync(nodeName + contractEventDetailsDto.Address, x =>
                    x.Address == contractEventDetailsDto.Address
                    && x.Pid == eventDetailsEto.Pid
                    && x.ChainId == chain.Id);

            var userInfo = await _userRepository.GetAsync(x =>
                x.GameOfTrustId == gameOfTrust.Id &&
                x.Address == eventDetailsEto.Receiver &&
                x.ChainId == chain.Id);

            var harvestToken = _tokenProvider.GetToken(gameOfTrust.HarvestTokenId);
            var amount = BigDecimal.Parse(eventDetailsEto.Amount.ToString()) /
                                BigInteger.Pow(10, harvestToken.Decimals);
            userInfo.ReceivedAmount = (BigDecimal.Parse(userInfo.ReceivedAmount) + amount).ToString();
            await _userRepository.UpdateAsync(userInfo);

            await _recordRepository.InsertAsync(new GameOfTrustRecord
            {
                Address = eventDetailsEto.Receiver,
                Amount = amount.ToString(),
                Timestamp = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp*1000),
                Type = BehaviorType.Harvest,
                ChainId = chain.Id,
                TransactionHash = contractEventDetailsDto.TransactionHash,
                GameOfTrustId = gameOfTrust.Id
            });
        }
    }
}