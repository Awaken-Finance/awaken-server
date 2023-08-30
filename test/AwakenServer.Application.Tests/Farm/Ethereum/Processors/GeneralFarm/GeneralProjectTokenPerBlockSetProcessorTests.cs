using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.GeneralFarm;
using AwakenServer.Farm;
using Shouldly;
using Xunit;

namespace AwakenServer.Farms.Ethereum.Tests
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

        private async Task GeneralProjectTokenPerBlockSetAsync(string farmAddress, long amount,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<ProjectTokenPerBlockSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new ProjectTokenPerBlockSet
            {
                NewProjectTokenPerBlock = new BigInteger(amount)
            }, GetDefaultEventContext(farmAddress, confirmStatus: confirmStatus));
        }
    }
}