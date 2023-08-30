using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Constants;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Applications.GameOfTrust
{
    public partial class GameOfTrustAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Harvest_Sashimi_Should_Success_Test()
        {
            await initPool_Sashimi();
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 1;
            var sender = GameOfTrustTestData.ADDRESS_USER1;
            var amount = BigInteger.Pow(10, tokenA.Decimals) * 10;
            var unlockBlock = 25000;
            await DepositAsync(
                contractAddress,
                pid,
                sender,
                amount);

            await UpToStandardAsync(
                contractAddress,
                pid,
                unlockBlock,
                10000000000000000);

            var lockAmount = BigDecimal.Parse(amount.ToString()) / BigInteger.Pow(10, tokenA.Decimals);
            var rewardRate = GameOfTrustTestData.RewardRate/10000;
            var unlockCycle = GameOfTrustTestData.UnlockCycle;
            var currentBlock = 30000;
            var harvestAmount = amount*(1+rewardRate)*(currentBlock-unlockBlock)/unlockCycle;
            
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            targetPool.TotalValueLocked.ShouldBe(lockAmount.ToString());
            var (_, esUserList) = await _esUserRepository.GetListAsync();
            var targetUser = esUserList.First(x =>
                x.Address == sender && x.GameOfTrust.Address == contractAddress && x.GameOfTrust.Pid == pid);
            targetUser.ValueLocked.ShouldBe(lockAmount.ToString());

            await HarvestAsync(
                contractAddress,
                pid,
                sender,
                harvestAmount,
                currentBlock);
            (_, esPoolList) = await _esGameRepository.GetListAsync();
            targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            targetPool.TotalValueLocked.ShouldBe(lockAmount.ToString());
            targetPool.FineAmount.ShouldBe(BigInteger.Zero.ToString());
            targetPool.UnlockHeight.ShouldBe(unlockBlock);
            
            (_, esUserList) = await _esUserRepository.GetListAsync();
             targetUser = esUserList.First(x =>
                x.Address == sender && x.GameOfTrust.Address == contractAddress && x.GameOfTrust.Pid == pid);
             targetUser.ReceivedAmount.ShouldBe(
                 (BigDecimal.Parse(harvestAmount.ToString()) / BigInteger.Pow(10, tokenB.Decimals)).ToString());
             targetUser.ValueLocked.ShouldBe(lockAmount.ToString());
             targetUser.ReceivedFineAmount.ShouldBe(BigInteger.Zero.ToString());
        }

        private async Task HarvestAsync(string contractAddress, int pid, string sender,
            BigInteger harvestAmount, long currentBlock)
        {
            var harvestProcesser = GetRequiredService<IEventHandlerTestProcessor<HarvestEVentDto>>();
            await harvestProcesser.HandleEventAsync(new HarvestEVentDto
                {
                    Pid = pid,
                    Amount = harvestAmount,
                    Receiver = sender
                },
                GetDefaultEventContext(contractAddress, currentBlock: currentBlock));
        }
    }
}