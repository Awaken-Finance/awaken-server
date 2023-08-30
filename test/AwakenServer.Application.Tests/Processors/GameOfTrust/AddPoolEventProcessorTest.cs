using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.Core.Enums;
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
        public async Task Add_Pool_Event_Test()
        {
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 0;
            var blocksDaily = 8749;
            var depositToken = tokenA.Address;
            var harvestToken = tokenB.Address;
            var marketCap = BigInteger.Parse("100000000000000000000");
            var rewardRate = 500;
            var startBlock = 5666666;
            var unlockCycle = 10000;
            var stakeEndBlock = 6666666;
            var totalAmountLimit = BigInteger.Parse("1000000000000000000");
            await AddPoolAsync(
                contractAddress,
                pid,
                marketCap,
                rewardRate,
                unlockCycle,
                totalAmountLimit,
                startBlock,
                stakeEndBlock,
                blocksDaily,
                depositToken,
                harvestToken
            );
            var (_, esPoolList) = await _esGameRepository.GetListAsync();
            var targetPool = esPoolList.First(x => x.Address == contractAddress && x.Pid == pid);
            var finalMarketCap = BigDecimal.Parse(marketCap.ToString()) / BigDecimal.Pow(10, tokenUSD.Decimals);

            targetPool.Pid.ShouldBe(0);
            targetPool.UnlockMarketCap.ShouldBe(finalMarketCap.ToString());
            targetPool.HarvestToken.Address.ShouldBe(harvestToken);
            targetPool.DepositToken.Address.ShouldBe(depositToken);
            targetPool.Address.ShouldBe(contractAddress);
            targetPool.TotalValueLocked.ShouldBe("0");
            targetPool.FineAmount.ShouldBe("0");
            targetPool.BlocksDaily.ShouldBe(blocksDaily);
            targetPool.RewardRate.ShouldBe(rewardRate.ToString());
            targetPool.UnlockCycle.ShouldBe(unlockCycle);
            var finalTotalAmountLimit = BigDecimal.Parse(totalAmountLimit.ToString()) /
                                        BigDecimal.Pow(10, tokenA.Decimals);

            targetPool.TotalAmountLimit.ShouldBe(finalTotalAmountLimit.ToString());
            targetPool.EndHeight.ShouldBe(stakeEndBlock);
        }


        private async Task AddPoolAsync(
            string contractAddress,
            int pid,
            BigInteger marketCap,
            int rewardRate,
            long unlockCycle,
            BigInteger totalAmountLimit,
            long startBlock,
            long stakeEndBlock,
            long blocksDaily,
            string depositToken,
            string harvestToken
        )
        {
            var addPoolProcessor = GetRequiredService<IEventHandlerTestProcessor<AddPoolEventDto>>();
            await addPoolProcessor.HandleEventAsync(new AddPoolEventDto
            {
                Pid = pid,
                BlocksDaily = blocksDaily,
                DepositToken = depositToken,
                HarvestToken = harvestToken,
                MarketCap = marketCap,
                RewardRate = rewardRate,
                StartBlock = startBlock,
                UnlockCycle = unlockCycle,
                StakeEndBlock = stakeEndBlock,
                TotalAmountLimit = totalAmountLimit,
            }, GetDefaultEventContext(contractAddress));
        }

        private ContractEventDetailsDto GetDefaultEventContext(string farmAddress, string txHash = null,
            long timestamp = 0, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed,
            long currentBlock = 0)
        {
            return new ContractEventDetailsDto
            {
                StatusEnum = confirmStatus,
                NodeName = GameOfTrustTestData.DefaultNodeName,
                Address = farmAddress,
                TransactionHash = txHash,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)/1000,
                BlockNumber = currentBlock
            };
        }
    }
}