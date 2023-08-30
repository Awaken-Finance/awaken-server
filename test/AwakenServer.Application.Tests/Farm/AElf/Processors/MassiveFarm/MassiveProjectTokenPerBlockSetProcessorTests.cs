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
        public async Task MassiveFarm_Set_Project_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var period1Amount = 3100013;
            var period2Amount = 1232141;
            await MassiveProjectTokenPerBlockSetAsync(farmAddress, period1Amount, period2Amount);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.ProjectTokenMinePerBlock1.ShouldBe(period1Amount.ToString());
            targetFarm.ProjectTokenMinePerBlock2.ShouldBe(period2Amount.ToString());
        }

        private async Task MassiveProjectTokenPerBlockSetAsync(string farmAddress, long period1Amount1, long periodAmount2)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<DistributeTokenPerBlockSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new DistributeTokenPerBlockSet
            {
                NewDistributeTokenPerBlock1 = period1Amount1,
                NewDistributeTokenPerBlock2 = periodAmount2
            }, GetDefaultEventContext(farmAddress));
        }
    }
}