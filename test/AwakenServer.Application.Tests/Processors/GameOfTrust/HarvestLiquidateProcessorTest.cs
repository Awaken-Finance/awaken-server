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
        public async Task Harvest_Liquidate_Should_Success_Test()
        {
            await initPool_Sashimi();
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 1;
            var sender1 = GameOfTrustTestData.ADDRESS_USER1;
            var sender2 = GameOfTrustTestData.ADDRESS_USER2;
            var sender3 = GameOfTrustTestData.ADDRESS_USER3;
            var amount1 = BigInteger.Pow(10, tokenA.Decimals) * 10;
            var amount2 = BigInteger.Pow(10, tokenA.Decimals) * 5;
            var fineRate2 = 0.1;
            var amount3 = BigInteger.Pow(10, tokenA.Decimals) * 15;
            var unlockCycle = GameOfTrustTestData.UnlockCycle;
            var unlockBlock = 25000;
            var totalLockAmount = BigDecimal.Parse((amount1 + amount2 + amount3).ToString()) /
                                  BigInteger.Pow(10, tokenA.Decimals);
            var harvestLiquidateBlock = 40000;
            var withdrawBlock2 = 25000;
            var amountSashimi2 = BigInteger.Parse
            (
                (
                    (BigDecimal) amount2 * (1 - fineRate2)
                ).ToString()
            );
            var fine2 = BigInteger.Parse(((BigDecimal) amount2 * fineRate2).ToString());
            
            
            await DepositAsync(
                contractAddress
                , pid,
                sender1,
                amount1
            );

            await DepositAsync(
                contractAddress
                , pid,
                sender2,
                amount2
            );

            await DepositAsync(
                contractAddress
                , pid,
                sender3,
                amount3
            );

            await UpToStandardAsync(
                contractAddress,
                pid,
                unlockBlock,
                10000000000000000);
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            targetPool.UnlockHeight.ShouldBe(unlockBlock);
            targetPool.TotalValueLocked.ShouldBe(totalLockAmount.ToString());

            await WithdrawAsync(
                contractAddress,
                pid,
                sender2,
                0,
                amountSashimi2,
                fine2,
                withdrawBlock2
            );
            (_, esPoolList) = await _esGameRepository.GetListAsync();
            targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            var totalResidualPrincipal = totalLockAmount - BigDecimal.Parse(amount2.ToString())/BigInteger.Pow(10,tokenA.Decimals );
            targetPool.FineAmount.ShouldBe((BigDecimal.Parse(fine2.ToString())/BigInteger.Pow(10,tokenB.Decimals)).ToString());
            targetPool.TotalValueLocked.ShouldBe(totalResidualPrincipal.ToString());

            var harvestAmount = BigInteger.Parse((fine2 * (amount1 / (totalResidualPrincipal*BigInteger.Pow(10,tokenA.Decimals)))).ToString());
            await HarvestLiquidateAsync(
                contractAddress,
                pid,
                sender1,
                harvestAmount,
                harvestLiquidateBlock);
            (_, esPoolList) = await _esGameRepository.GetListAsync();
            targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            var residualFine = fine2 - harvestAmount;
            targetPool.FineAmount.ShouldBe((BigDecimal.Parse(residualFine.ToString())/BigInteger.Pow(10,tokenB.Decimals)).ToString());
            targetPool.TotalValueLocked.ShouldBe(totalResidualPrincipal.ToString());
            var (_, esUserList) = await _esUserRepository.GetListAsync();
            var targetUser = esUserList.First(x =>
                x.Address == sender1 && x.GameOfTrust.Address == contractAddress && x.GameOfTrust.Pid == pid);
            
            targetUser.ValueLocked.ShouldBe((BigDecimal.Parse(amount1.ToString())/BigInteger.Pow(10,tokenA.Decimals)).ToString());
            targetUser.ReceivedAmount.ShouldBe(BigInteger.Zero.ToString());
            targetUser.ReceivedFineAmount.ShouldBe((BigDecimal.Parse(harvestAmount.ToString())/BigInteger.Pow(10,tokenB.Decimals)).ToString());
        }
    
        private async Task HarvestLiquidateAsync(string contractAddress, int pid, string sender1, BigInteger amount1,
            int harvestLiquidateBlock)
        {
            var harvestLiquidateProcessor =
                GetRequiredService<IEventHandlerTestProcessor<HarvestLiquidateRewardEventDto>>();

            await harvestLiquidateProcessor.HandleEventAsync(
                new HarvestLiquidateRewardEventDto
                {
                    Pid = pid,
                    Receiver = sender1,
                    Amount = amount1
                }, GetDefaultEventContext(contractAddress,currentBlock:harvestLiquidateBlock));

        }
    }
}