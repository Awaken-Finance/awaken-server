using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using Xunit;
using Awaken.Contracts.Farm;
using AwakenServer.Farm;
using Shouldly;

namespace AwakenServer.Farms.AElf.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Add_Pool_Should_Contain_Pool_Info()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);
            var (_, esFarmPool) = await _esPoolRepository.GetListAsync();
            var targetPool = esFarmPool.First(x => x.FarmAddress == farmAddress && x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastRewardBlock);
            targetPool.SwapToken.Symbol.ShouldBe(tokenSymbol);
            targetPool.PoolType.ShouldBe(poolType);
            targetPool.Pid.ShouldBe(pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.AccumulativeDividendUsdt.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.SwapToken.Id.ShouldNotBe(Guid.Empty);
            targetPool.Weight.ShouldBe(weight);
        }

        [Fact(Skip = "no need")]
        public async Task Add_Token_Pool_Should_Contain_Pool_Info()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenThreeSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);
            var (_, esFarmPool) = await _esPoolRepository.GetListAsync();
            var targetPool = esFarmPool.First(x => x.FarmAddress == farmAddress && x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastRewardBlock);
            targetPool.SwapToken.Symbol.ShouldBe(tokenSymbol);
            targetPool.PoolType.ShouldBe(poolType);
            targetPool.Pid.ShouldBe(pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.AccumulativeDividendUsdt.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.SwapToken.Id.ShouldNotBe(Guid.Empty);
            targetPool.Weight.ShouldBe(weight);
        }

        [Fact(Skip = "no need")]
        public async Task Add_Non_Tokens_Pool_Should_Contain_Pool_Info()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenFourSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);
            var (_, esFarmPool) = await _esPoolRepository.GetListAsync();
            var targetPool = esFarmPool.First(x => x.FarmAddress == farmAddress && x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastRewardBlock);
            targetPool.SwapToken.Symbol.ShouldBe(tokenSymbol);
            targetPool.PoolType.ShouldBe(poolType);
            targetPool.Pid.ShouldBe(pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.AccumulativeDividendUsdt.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);
            targetPool.SwapToken.Id.ShouldNotBe(Guid.Empty);
            targetPool.Weight.ShouldBe(weight);
        }


        [Fact(Skip = "no need")]
        public async Task Add_Pool_Should_Modify_Farm_Weight()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 1001;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);
            var (farmCount, farms) = await _esFarmRepository.GetListAsync();
            farmCount.ShouldBe(1);
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.TotalWeight.ShouldBe(weight);
        }

        private async Task AddPoolAsync(string farmAddress, int pid, PoolType poolType,
            string tokenSymbol, long lastRewardBlock, int weight)
        {
            var poolAddedProcessor = GetRequiredService<IEventHandlerTestProcessor<PoolAdded>>();
            await poolAddedProcessor.HandleEventAsync(new PoolAdded
            {
                Token = tokenSymbol,
                LastRewardBlockHeight = lastRewardBlock,
                PoolType = (int) poolType,
                Pid = pid,
                AllocationPoint = weight,
            }, GetDefaultEventContext(farmAddress));
        }

        private EventContext GetDefaultEventContext(string farmAddress,
            string txHash = null, long blockTime = 0, string status = "Mined")
        {
            var timestamp = blockTime > 0 ? DateTimeHelper.FromUnixTimeMilliseconds(blockTime) : DateTime.UtcNow;
            return new EventContext
            {
                Status = status,
                ChainId = DefaultChainAElfId,
                EventAddress = farmAddress,
                TransactionId = txHash,
                BlockTime = timestamp
            };
        }
    }
}