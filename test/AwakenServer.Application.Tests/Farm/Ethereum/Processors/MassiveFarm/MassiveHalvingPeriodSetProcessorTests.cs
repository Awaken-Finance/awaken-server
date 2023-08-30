using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.MassiveFarm;
using AwakenServer.Farm;
using Shouldly;
using Xunit;

namespace AwakenServer.Farms.Ethereum.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task MassiveFarm_Set_Period_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var newPeriod1 = 3100013;
            var newPeriod2 = 313;

            await MassiveHalvingPeriodSetAsync(farmAddress, newPeriod1, newPeriod2,
                ContractEventStatus.Unconfirmed);
            var (farmsCount, _) = await _esFarmRepository.GetListAsync();
            farmsCount.ShouldBe(0);

            await MassiveHalvingPeriodSetAsync(farmAddress, newPeriod1, newPeriod2);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.MiningHalvingPeriod1.ShouldBe(newPeriod1);
            targetFarm.MiningHalvingPeriod2.ShouldBe(newPeriod2);
        }

        private async Task MassiveHalvingPeriodSetAsync(string farmAddress, long period1, long period2,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<HalvingPeriodSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new HalvingPeriodSet
            {
                Period1 = period1,
                Period2 = period2
            }, GetDefaultEventContext(farmAddress, confirmStatus: confirmStatus));
        }
    }
}