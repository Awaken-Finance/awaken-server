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
        public async Task MassiveFarm_Set_Period_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var newPeriod1 = 3100013;
            var newPeriod2 = 313;

            await MassiveHalvingPeriodSetAsync(farmAddress, newPeriod1, newPeriod2);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.MiningHalvingPeriod1.ShouldBe(newPeriod1);
            targetFarm.MiningHalvingPeriod2.ShouldBe(newPeriod2);
        }

        private async Task MassiveHalvingPeriodSetAsync(string farmAddress, long period1, long period2)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<HalvingPeriodSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new HalvingPeriodSet
            {
                Period1 = period1,
                Period2 = period2
            }, GetDefaultEventContext(farmAddress));
        }
    }
}