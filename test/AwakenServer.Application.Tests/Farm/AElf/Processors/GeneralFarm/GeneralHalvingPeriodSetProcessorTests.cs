using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AwakenServer.Farm;
using Xunit;
using Awaken.Contracts.PoolTwoContract;
using Shouldly;

namespace AwakenServer.Farms.AElf.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task GeneralFarm_Set_Period_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var newPeriod = 3100013;
            await GeneralHalvingPeriodSetAsync(farmAddress, newPeriod);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.MiningHalvingPeriod1.ShouldBe(newPeriod);
            targetFarm.MiningHalvingPeriod2.ShouldBe(0);
        }

        private async Task GeneralHalvingPeriodSetAsync(string farmAddress, long period)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<HalvingPeriodSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new HalvingPeriodSet
            {
                Period = period
            }, GetDefaultEventContext(farmAddress));
        }
    }
}