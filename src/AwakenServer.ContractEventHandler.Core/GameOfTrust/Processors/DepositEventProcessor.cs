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
using AwakenServer.GameOfTrust;
using AwakenServer.Tokens;
using Microsoft.Extensions.Logging;
using Nethereum.Util;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Processors
{
    public class DepositEventProcessor : EthereumEthereumEventProcessorBase<DepositEventDto>
    {
        private readonly IRepository<UserGameOfTrust> _userRepository;
        private readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> _gameRepository;
        private readonly IRepository<GameOfTrustRecord> _recordRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ILogger<DepositEventProcessor> _logger;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ITokenAppService _tokenAppService;

        public DepositEventProcessor(IRepository<UserGameOfTrust> userRepository,
            IRepository<Entities.GameOfTrust.Ef.GameOfTrust> gameRepository,
            IRepository<GameOfTrustRecord> recordRepository, IChainAppService chainAppService,
            ILogger<DepositEventProcessor> logger, IUnitOfWorkManager unitOfWorkManager,
            ITokenAppService tokenAppService)
        {
            _userRepository = userRepository;
            _gameRepository = gameRepository;
            _recordRepository = recordRepository;
            _chainAppService = chainAppService;
            _logger = logger;
            _unitOfWorkManager = unitOfWorkManager;
            _tokenAppService = tokenAppService;
        }

        protected override async Task HandleEventAsync(DepositEventDto eventDetailsEto,
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

            _logger.LogInformation("Deposit event process income TxHash:" + contractEventDetailsDto.TransactionHash);
            var nodeName = contractEventDetailsDto.NodeName;
            using var uow = _unitOfWorkManager.Begin(true, true, IsolationLevel.ReadCommitted);
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);


            var gameOfTrust = await _gameRepository.GetAsync(x =>
                x.Address == contractEventDetailsDto.Address &&
                x.Pid == eventDetailsEto.Pid &&
                x.ChainId == chain.Id);
            var depositToken = await _tokenAppService.GetAsync(gameOfTrust.DepositTokenId);

            gameOfTrust.TotalValueLocked = (BigDecimal.Parse(gameOfTrust.TotalValueLocked) +
                                            BigDecimal.Parse(eventDetailsEto.Amount.ToString()) /
                                            BigInteger.Pow(10, depositToken.Decimals)).ToString();
            await _gameRepository.UpdateAsync(gameOfTrust);

            var record = new GameOfTrustRecord
            {
                Type = BehaviorType.Deposit,
                Address = eventDetailsEto.Sender,
                Amount = (BigDecimal.Parse(eventDetailsEto.Amount.ToString()) /
                          BigInteger.Pow(10, depositToken.Decimals)).ToString(),
                Timestamp = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp*1000),
                ChainId = chain.Id,
                TransactionHash = contractEventDetailsDto.TransactionHash,
                GameOfTrustId = gameOfTrust.Id
            };
            await _recordRepository.InsertAsync(record);

            var userInfo = await _userRepository.FirstOrDefaultAsync(x => x.GameOfTrustId == gameOfTrust.Id
                                                                          && x.Address == eventDetailsEto.Sender
                                                                          && x.ChainId == chain.Id);
            if (userInfo != null)
            {
                userInfo.ValueLocked =
                    (BigDecimal.Parse(userInfo.ValueLocked) + BigDecimal.Parse(eventDetailsEto.Amount.ToString()) /
                        BigInteger.Pow(10, depositToken.Decimals)).ToString();
                await _userRepository.UpdateAsync(userInfo);
            }
            else
            {
                await _userRepository.InsertAsync(new UserGameOfTrust
                {
                    Address = eventDetailsEto.Sender,
                    ChainId = chain.Id,
                    ReceivedAmount = "0",
                    ValueLocked = (BigDecimal.Parse(eventDetailsEto.Amount.ToString()) /
                                   BigInteger.Pow(10, depositToken.Decimals)).ToString(),
                    ReceivedFineAmount = "0",
                    GameOfTrustId = gameOfTrust.Id
                });
            }

            await uow.CompleteAsync();
        }
    }
}