using System;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task WeightSet_Should_Modify_Farm_And_Pool()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid && farmAddress == x.FarmAddress);
            targetPool.Weight.ShouldBe(weight);

            var (farmsCount, _) = await _esFarmRepository.GetListAsync();
            farmsCount.ShouldBe(0);

            var newWeight = 100;
            await WeightSetAsync(farmAddress, pid, newWeight);
            (_, pools) = await _esPoolRepository.GetListAsync();
            targetPool = pools.First(x => x.Pid == pid && farmAddress == x.FarmAddress);
            targetPool.Weight.ShouldBe(newWeight);

            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.TotalWeight.ShouldBe(newWeight);

            newWeight = 0;
            await WeightSetAsync(farmAddress, pid, newWeight);

            (_, pools) = await _esPoolRepository.GetListAsync();
            targetPool = pools.First(x => x.Pid == pid && farmAddress == x.FarmAddress);
            targetPool.Weight.ShouldBe(newWeight);

            (_, farms) = await _esFarmRepository.GetListAsync();
            targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.TotalWeight.ShouldBe(newWeight);
        }

        private async Task WeightSetAsync(string farmAddress, int pid, int newWeight)
        {
            var weightSetProcessor = GetRequiredService<IEventHandlerTestProcessor<WeightSet>>();
            await weightSetProcessor.HandleEventAsync(new WeightSet
            {
                Pid = pid,
                NewAllocationPoint = newWeight
            }, GetDefaultEventContext(farmAddress));
        }
    }
}