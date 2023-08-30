using System;
using System.Linq;
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
        public async Task GeneralFarm_Set_Period_Should_Update_Farm()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var newPeriod = 3100013;
            
            await GeneralHalvingPeriodSetAsync(farmAddress, newPeriod, ContractEventStatus.Unconfirmed);
            var (farmsCount, _) = await _esFarmRepository.GetListAsync();
            farmsCount.ShouldBe(0);
            
            await GeneralHalvingPeriodSetAsync(farmAddress, newPeriod);
            var (_, farms) = await _esFarmRepository.GetListAsync();
            var targetFarm = farms.First(x => x.FarmAddress == farmAddress);
            targetFarm.MiningHalvingPeriod1.ShouldBe(newPeriod);
            targetFarm.MiningHalvingPeriod2.ShouldBe(0);
        }

        private async Task GeneralHalvingPeriodSetAsync(string farmAddress, long period,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var tokenPerBlockSetProcessor = GetRequiredService<IEventHandlerTestProcessor<HalvingPeriodSet>>();
            await tokenPerBlockSetProcessor.HandleEventAsync(new HalvingPeriodSet
            {
                Period = period
            }, GetDefaultEventContext(farmAddress, confirmStatus: confirmStatus));
        }
    }
}