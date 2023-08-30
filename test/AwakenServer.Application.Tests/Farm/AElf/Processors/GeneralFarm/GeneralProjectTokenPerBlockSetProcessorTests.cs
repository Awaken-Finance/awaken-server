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
        public async Task GeneralFarm_Set_ProjectToken_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var amount = 3100013;
            await GeneralProjectTokenPerBlockSetAsync(farmAddress, amount);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.ProjectTokenMinePerBlock1.ShouldBe(amount.ToString());
            targetFarm.ProjectTokenMinePerBlock2.ShouldBe(FarmTestData.ZeroBalance);
        }

        private async Task GeneralProjectTokenPerBlockSetAsync(string farmAddress, long amount)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<DistributeTokenPerBlockSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new DistributeTokenPerBlockSet
            {
                NewDistributeTokenPerBlock = amount
            }, GetDefaultEventContext(farmAddress));
        }
    }
}