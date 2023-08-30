using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs;
using AwakenServer.Farm;
using Shouldly;
using Xunit;

namespace AwakenServer.Farms.Ethereum.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task WeightSet_Should_Modify_Farm_And_Pool()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid && farmAddress == x.FarmAddress);
            targetPool.Weight.ShouldBe(weight);

            var (farmsCount, _) = await _esFarmRepository.GetListAsync();
            farmsCount.ShouldBe(0);

            var newWeight = 100;
            await WeightSetAsync(farmAddress, pid, newWeight, ContractEventStatus.Unconfirmed);
            (_, pools) = await _esPoolRepository.GetListAsync();
            targetPool = pools.FirstOrDefault(x => x.Pid == pid && farmAddress == x.FarmAddress);
            targetPool.Weight.ShouldNotBe(newWeight);
            
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

        private async Task WeightSetAsync(string farmAddress, int pid, int newWeight,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var weightSetProcessor = GetRequiredService<IEventHandlerTestProcessor<WeightSet>>();
            await weightSetProcessor.HandleEventAsync(new WeightSet
            {
                Pid = pid,
                NewAllocationPoint = newWeight
            }, GetDefaultEventContext(farmAddress, confirmStatus: confirmStatus));
        }
    }
}