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
using IdentityServer4.Extensions;
using Nethereum.Util;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Processors
{
    public class WithdrawEventProcessor : EthereumEthereumEventProcessorBase<WithdrawEventDto>
    {
        private readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> _gameRepository;
        private readonly IRepository<UserGameOfTrust> _userRepository;
        private readonly IRepository<GameOfTrustRecord> _recordRepository;
        private readonly IChainAppService _chainAppService;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ITokenProvider _tokenProvider;


        public WithdrawEventProcessor(IRepository<Entities.GameOfTrust.Ef.GameOfTrust> gameRepository,
            IRepository<UserGameOfTrust> userRepository, IRepository<GameOfTrustRecord> recordRepository,
            IChainAppService chainAppService,
            IUnitOfWorkManager unitOfWorkManager, ITokenProvider tokenProvider)
        {
            _gameRepository = gameRepository;
            _userRepository = userRepository;
            _recordRepository = recordRepository;
            _chainAppService = chainAppService;
            _unitOfWorkManager = unitOfWorkManager;
            _tokenProvider = tokenProvider;
        }


        protected override async Task HandleEventAsync(WithdrawEventDto eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            string nodeName = contractEventDetailsDto.NodeName;
            using var uow = _unitOfWorkManager.Begin(
                requiresNew: true, isTransactional: true, isolationLevel: IsolationLevel.ReadCommitted
            );
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);

            var gameOfTrust = await _gameRepository.GetAsync(x =>
                x.Address == contractEventDetailsDto.Address && x.Pid == eventDetailsEto.Pid &&
                x.ChainId == chain.Id);
            var depositToken =  _tokenProvider.GetToken(gameOfTrust.DepositTokenId);
            var harvestToken =  _tokenProvider.GetToken(gameOfTrust.HarvestTokenId);
            var userInfo = await _userRepository.FirstOrDefaultAsync(x => x.GameOfTrustId == gameOfTrust.Id
                                                                          && x.Address == eventDetailsEto.Receiver
                                                                          && x.ChainId == chain.Id);
            var withdrawRecord = new GameOfTrustRecord();
            var harvestRecord = new GameOfTrustRecord();
            if (userInfo != null)
            {
                var valueLocked = userInfo.ValueLocked;

                // 1. stake period end
                if (gameOfTrust.EndHeight < contractEventDetailsDto.BlockNumber)
                {
                    if (gameOfTrust.UnlockHeight > 0)
                    {
                        var endBlock = gameOfTrust.UnlockHeight + gameOfTrust.UnlockCycle;
                        var countBlock = contractEventDetailsDto.BlockNumber > endBlock
                            ? endBlock
                            : contractEventDetailsDto.BlockNumber;
                        if (gameOfTrust.DepositTokenId == gameOfTrust.HarvestTokenId)
                        {
                            var passBlockProportion =
                                (countBlock -
                                 BigInteger.Parse(gameOfTrust.UnlockHeight.ToString())) /
                                BigDecimal.Parse(gameOfTrust.UnlockCycle.ToString());
                            var residualPrincipal = BigDecimal.Parse(userInfo.ValueLocked) * (BigInteger.One-passBlockProportion);
                            var harvestPart =
                                BigDecimal.Parse(eventDetailsEto.AmountProjectToken.ToString()) /
                                BigInteger.Pow(10, harvestToken.Decimals) - residualPrincipal;
                            userInfo.ReceivedAmount =
                                (BigDecimal.Parse(userInfo.ReceivedAmount) + harvestPart).ToString();
                            withdrawRecord.Amount = residualPrincipal.ToString();
                            harvestRecord.Amount = harvestPart.ToString();
                        }
                        else
                        {
                            var harvestPart = BigDecimal.Parse(eventDetailsEto.AmountProjectToken.ToString()) /
                                              BigInteger.Pow(10, harvestToken.Decimals);
                            var residualPrincipal = BigDecimal.Parse(eventDetailsEto.AmountToken.ToString()) /
                                                    BigInteger.Pow(10, depositToken.Decimals);
                            userInfo.ReceivedAmount = (BigDecimal.Parse(userInfo.ReceivedAmount) + harvestPart).ToString();
                            withdrawRecord.Amount = residualPrincipal.ToString();
                            harvestRecord.Amount = harvestPart.ToString();
                        }

                        harvestRecord.Address = eventDetailsEto.Receiver;
                        harvestRecord.Timestamp =
                            DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp * 1000);
                        harvestRecord.Type = BehaviorType.Harvest;
                        harvestRecord.TransactionHash = contractEventDetailsDto.TransactionHash;
                        harvestRecord.ChainId = chain.Id;
                        harvestRecord.GameOfTrustId = gameOfTrust.Id;
                    }
                    else
                    {
                        if (gameOfTrust.DepositTokenId == gameOfTrust.HarvestTokenId)
                        {
                            var residualPrincipal = BigDecimal.Parse(eventDetailsEto.AmountProjectToken.ToString()) /
                                                    BigInteger.Pow(10, depositToken.Decimals);
                            withdrawRecord.Amount = residualPrincipal.ToString();
                        }
                        else
                        {
                            var residualPrincipal = BigDecimal.Parse(eventDetailsEto.AmountToken.ToString()) /
                                                    BigInteger.Pow(10, depositToken.Decimals);
                            withdrawRecord.Amount = residualPrincipal.ToString();
                        }
                    }

                    userInfo.ValueLocked = "0";
                    gameOfTrust.TotalValueLocked =
                        (BigDecimal.Parse(gameOfTrust.TotalValueLocked) - BigDecimal.Parse(valueLocked)).ToString();
                }
                else
                {
                    // 2.stake period tokenA->tokenB
                    if (gameOfTrust.DepositTokenId != gameOfTrust.HarvestTokenId)
                    {
                        var withdrawAmount = (BigDecimal) (eventDetailsEto.AmountToken + eventDetailsEto.Fine) /
                                             BigInteger.Pow(10, depositToken.Decimals);
                        userInfo.ValueLocked = (BigDecimal.Parse(userInfo.ValueLocked) - withdrawAmount).ToString();
                        gameOfTrust.TotalValueLocked =
                            (BigDecimal.Parse(gameOfTrust.TotalValueLocked) - withdrawAmount).ToString();
                        withdrawRecord.Amount =
                            (eventDetailsEto.AmountToken / BigInteger.Pow(10, depositToken.Decimals)).ToString();
                    }
                    else
                    {
                        var withdrawAmount = (BigDecimal) (eventDetailsEto.AmountProjectToken + eventDetailsEto.Fine) /
                                             BigInteger.Pow(10, depositToken.Decimals);
                        userInfo.ValueLocked = (BigDecimal.Parse(userInfo.ValueLocked) - withdrawAmount).ToString();
                        gameOfTrust.TotalValueLocked =
                            (BigDecimal.Parse(gameOfTrust.TotalValueLocked) - withdrawAmount).ToString();
                        withdrawRecord.Amount =
                            ((BigDecimal) eventDetailsEto.AmountProjectToken / BigInteger.Pow(10, depositToken.Decimals))
                            .ToString();
                    }
                }

                gameOfTrust.FineAmount =
                    (BigDecimal.Parse(gameOfTrust.FineAmount) + BigDecimal.Parse(eventDetailsEto.Fine.ToString()) /
                        BigInteger.Pow(10, harvestToken.Decimals)).ToString();
                await _userRepository.UpdateAsync(userInfo);
            }

            await _gameRepository.UpdateAsync(gameOfTrust);
            withdrawRecord.Address = eventDetailsEto.Receiver;
            withdrawRecord.GameOfTrustId = gameOfTrust.Id;
            withdrawRecord.Timestamp =
                DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp * 1000);
            withdrawRecord.ChainId = chain.Id;
            withdrawRecord.TransactionHash = contractEventDetailsDto.TransactionHash;
            withdrawRecord.Type = BehaviorType.Withdraw;
            await _recordRepository.InsertAsync(withdrawRecord);
            if (!harvestRecord.Amount.IsNullOrEmpty())
            {
                await _recordRepository.InsertAsync(harvestRecord);
            }

            await uow.CompleteAsync();
        }
    }
}