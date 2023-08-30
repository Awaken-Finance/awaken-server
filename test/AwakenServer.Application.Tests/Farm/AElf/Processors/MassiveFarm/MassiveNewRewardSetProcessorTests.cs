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
        public async Task MassiveFarm_Set_Reward_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var startBlock = 310;
            var endBlock = 313324;
            var usdtAmount = 123120;

            await MassiveHalvingNewRewardSetAsync(farmAddress, startBlock, endBlock, usdtAmount);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.UsdtDividendStartBlockHeight.ShouldBe(startBlock);
            targetFarm.UsdtDividendEndBlockHeight.ShouldBe(endBlock);
            targetFarm.UsdtDividendPerBlock.ShouldBe(usdtAmount.ToString());
        }

        private async Task MassiveHalvingNewRewardSetAsync(string farmAddress, long startBlock, long endBlock,
            long totalAmount)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<NewRewardSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new NewRewardSet
            {
                StartBlock = startBlock,
                EndBlock = endBlock,
                UsdtPerBlock = totalAmount
            }, GetDefaultEventContext(farmAddress));
        }
    }
}