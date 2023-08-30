using System;
using System.Linq;
using System.Numerics;
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
        public async Task MassiveFarm_Set_Token_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            // var pid = 0;
            // var poolType = PoolType.Massive;
            // var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            // var lastRewardBlock = 1230;
            // var weight = 0;
            // await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);

            var period1Amount = 3100013;
            var period2Amount = 1232141;
            await MassiveTokenPerBlockSetAsync(farmAddress, period1Amount, period2Amount,
                ContractEventStatus.Unconfirmed);
            var (farmsCount, _) = await _esFarmRepository.GetListAsync();
            farmsCount.ShouldBe(0);
            // var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            // targetFarm.TokenMinePerBlock1.ShouldNotBe(period1Amount.ToString());

            await MassiveTokenPerBlockSetAsync(farmAddress, period1Amount, period2Amount);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.ProjectTokenMinePerBlock1.ShouldBe(period1Amount.ToString());
            targetFarm.ProjectTokenMinePerBlock2.ShouldBe(period2Amount.ToString());
        }

        private async Task MassiveTokenPerBlockSetAsync(string farmAddress, long period1Amount, long periodAmount2,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<ProjectTokenPerBlockSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new ProjectTokenPerBlockSet
            {
                NewProjectTokenPerBlock1 = new BigInteger(period1Amount),
                NewProjectTokenPerBlock2 = new BigInteger(periodAmount2)
            }, GetDefaultEventContext(farmAddress, confirmStatus: confirmStatus));
        }
    }
}