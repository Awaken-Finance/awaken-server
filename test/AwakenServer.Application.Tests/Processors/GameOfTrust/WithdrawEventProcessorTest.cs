using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Constants;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using AwakenServer.GameOfTrust;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Applications.GameOfTrust
{
    public partial class GameOfTrustAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Withdraw_Sashimi_StarkePeriod_Should_Success_Test()
        {
            await initPool_Sashimi();
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 1;
            var sender = GameOfTrustTestData.ADDRESS_USER1;
            var amount = BigInteger.Pow(10, tokenA.Decimals) * 10;
            var amountSashimi = amount / 2;
            var Fine = 0;
            await DepositAsync(
                contractAddress,
                pid,
                sender,
                amount);
            await WithdrawAsync(contractAddress,
                pid,
                sender,
                0,
                amountSashimi,
                Fine,
                15000
            );
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            var (_, esUserList) = await _esUserRepository.GetListAsync();
            var targetUser = esUserList.First(x =>
                x.Address == sender && x.GameOfTrust.Address == contractAddress && x.GameOfTrust.Pid == pid);
            var (_, esUserRecordList) = await _esUserRecordRepository.GetListAsync();
            var targetRecord =
                esUserRecordList.First(x => x.GameOfTrust.Address == contractAddress
                                            && x.Address == sender
                                            && x.Type == BehaviorType.Withdraw);
            var surplusAmount = (BigDecimal) (amount - amountSashimi) / BigDecimal.Pow(10, tokenA.Decimals);

            targetPool.TotalValueLocked.ShouldBe(surplusAmount.ToString());
            targetPool.FineAmount.ShouldBe(BigInteger.Zero.ToString());
            targetUser.ValueLocked.ShouldBe(surplusAmount.ToString());
            targetUser.ReceivedFineAmount.ShouldBe(BigInteger.Zero.ToString());
            targetRecord.Amount.ShouldBe(surplusAmount.ToString());
        }

        /**
         * It will withdraw all tokens once withdraw in the period. 
         */
        [Fact(Skip = "no need")]
        public async Task Withdraw_Sashimi_StakeEnd_Locked_Should_Success_Test()
        {
            await initPool_Sashimi();
            var fineRate = 0.1;
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 1;
            var sender = GameOfTrustTestData.ADDRESS_USER1;
            var amount = BigInteger.Pow(10, tokenA.Decimals) * 10;
            var amountSashimi = BigInteger.Parse(((BigDecimal) amount * (1 - fineRate)).ToString());
            var fine = BigInteger.Parse(((BigDecimal) amount * fineRate).ToString());
            await DepositAsync(
                contractAddress,
                pid,
                sender,
                amount);

            await WithdrawAsync(
                contractAddress,
                pid,
                sender,
                0,
                amountSashimi,
                fine,
                25000
            );
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            var (_, esUserList) = await _esUserRepository.GetListAsync();
            var targetUser = esUserList.First(x =>
                x.Address == sender && x.GameOfTrust.Address == contractAddress && x.GameOfTrust.Pid == pid);
            var (_, esUserRecordList) = await _esUserRecordRepository.GetListAsync();
            var targetRecord =
                esUserRecordList.First(x => x.GameOfTrust.Address == contractAddress
                                            && x.Address == sender
                                            && x.Type == BehaviorType.Withdraw);
            targetPool.TotalValueLocked.ShouldBe(BigInteger.Zero.ToString());
            targetPool.FineAmount.ShouldBe((fine / BigInteger.Pow(10, tokenA.Decimals)).ToString());
            targetUser.ValueLocked.ShouldBe(BigInteger.Zero.ToString());
            targetUser.ReceivedFineAmount.ShouldBe(BigInteger.Zero.ToString());
            var withdrawFinal = amount - fine;
            targetRecord.Amount.ShouldBe(((BigDecimal) withdrawFinal / BigInteger.Pow(10, tokenA.Decimals))
                .ToString());
        }

        /**
         * Withdraw in unlock period
         */
        [Fact(Skip = "no need")]
        public async Task Withdraw_Sashimi_Unlocked_Not_Finshed_Should_Success_Test()
        {
            await initPool_Sashimi();
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 1;
            var sender = GameOfTrustTestData.ADDRESS_USER1;
            var amount = BigInteger.Pow(10, tokenA.Decimals) * 10;
            var unlockCycle = GameOfTrustTestData.UnlockCycle;
            await DepositAsync(
                contractAddress
                , pid,
                sender,
                amount
            );

            var unlockBlock = 25000;
            await UpToStandardAsync(
                contractAddress,
                pid,
                unlockBlock,
                10000000000000000);
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            targetPool.UnlockHeight.ShouldBe(unlockBlock);
            var withdrawBlock = 30000;
            var rewardRate = (BigDecimal) GameOfTrustTestData.RewardRate / 10000;
            // withdraw event
            var amountToken =
                BigInteger.Parse((amount * (1 + rewardRate) * (withdrawBlock - unlockBlock) / (BigInteger) unlockCycle)
                    .ToString());
            var amountSashimi = amount * (unlockBlock + unlockCycle - withdrawBlock) / unlockCycle;
            var fine = BigInteger.Parse((amount * (1 + rewardRate) - amountToken - amountSashimi).ToString());

            await WithdrawAsync(
                contractAddress,
                pid,
                sender,
                amountToken,
                amountSashimi,
                fine,
                withdrawBlock
            );

            var (_, esPoolList2) = await _esGameRepository.GetListAsync();
            var targetPool2 = esPoolList2.First(x => x.Address == contractAddress && x.Pid == pid);
            var (_, esUserList) = await _esUserRepository.GetListAsync();
            var targetUser = esUserList.First(x =>
                x.Address == sender && x.GameOfTrust.Address == contractAddress && x.GameOfTrust.Pid == pid);
            targetPool2.FineAmount.ShouldBe(
                (BigDecimal.Parse(fine.ToString()) / BigDecimal.Pow(10, tokenA.Decimals)).ToString());
            targetPool2.TotalValueLocked.ShouldBe(BigInteger.Zero.ToString());
            targetUser.ReceivedAmount.ShouldBe(
                (BigDecimal.Parse(amountToken.ToString()) / BigDecimal.Pow(10, tokenA.Decimals)).ToString());
            targetUser.ReceivedFineAmount.ShouldBe(BigInteger.Zero.ToString());
            var (_, esUserRecordList) = await _esUserRecordRepository.GetListAsync();
            var targetWithdrawRecord =
                esUserRecordList.First(x => x.GameOfTrust.Address == contractAddress
                                            && x.Address == sender
                                            && x.Type == BehaviorType.Withdraw);
            var targetHarvestRecord =
                esUserRecordList.First(x => x.GameOfTrust.Address == contractAddress
                                            && x.Address == sender
                                            && x.Type == BehaviorType.Harvest);
            targetWithdrawRecord.Amount.ShouldBe(
                (BigDecimal.Parse(amountSashimi.ToString()) / BigInteger.Pow(10, tokenA.Decimals)).ToString());
            targetHarvestRecord.Amount.ShouldBe(
                (BigDecimal.Parse(amountToken.ToString()) / BigInteger.Pow(10, tokenB.Decimals)).ToString());
        }


        private async Task WithdrawAsync(string contractAddress, int pid, string sender, BigInteger amountToken,
            BigInteger amountSashimi, BigInteger fine, long blockNumber)
        {
            var withdrawProcessor = GetRequiredService<IEventHandlerTestProcessor<WithdrawEventDto>>();
            await withdrawProcessor.HandleEventAsync(new WithdrawEventDto
            {
                Fine = fine,
                Pid = pid,
                Receiver = sender,
                AmountProjectToken = amountToken,
                AmountToken = amountSashimi
            }, GetDefaultEventContext(contractAddress, currentBlock: blockNumber));
        }
    }
}