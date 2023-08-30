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
        public async Task GeneralFarm_Pool_Update_Should_Modify_ProjectToken_And_Usdt()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var pid = 0;
            var poolType = PoolType.Normal;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);

            var tokenAmount = 1000213;
            long lastUpdateHeight = 1000999;
            
            await UpdateGeneralFarmPool(farmAddress, pid, tokenAmount, lastUpdateHeight, ContractEventStatus.Unconfirmed);
            (_, pools) = await _esPoolRepository.GetListAsync();
            targetPool = pools.First(x => x.Pid == pid);
            targetPool.AccumulativeDividendProjectToken.ShouldNotBe(tokenAmount.ToString());
            
            await UpdateGeneralFarmPool(farmAddress, pid, tokenAmount, lastUpdateHeight);
            (_, pools) = await _esPoolRepository.GetListAsync();
            targetPool = pools.First(x => x.Pid == pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(tokenAmount.ToString());
        }

        [Fact(Skip = "no need")]
        public async Task GeneralFarm_Pool_Update_Twice_Should_Update_Latest_Height()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var pid = 0;
            var poolType = PoolType.Normal;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            
            var projectTokenAmount = 1000213;
            long lastUpdateHeight = 1000999;
            long smallerHeight = 213;
            await UpdateGeneralFarmPool(farmAddress, pid, projectTokenAmount, lastUpdateHeight);
            await UpdateGeneralFarmPool(farmAddress, pid, projectTokenAmount, smallerHeight);
            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastUpdateHeight);
        }

        private async Task UpdateGeneralFarmPool(string farmAddress, int pid, long projectTokenAmount, long lastUpdateHeight,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var updatePoolProcessor = GetRequiredService<IEventHandlerTestProcessor<UpdatePool>>();
            await updatePoolProcessor.HandleEventAsync(new UpdatePool
            {
                Pid = pid,
                ProjectTokenAmount = new BigInteger(projectTokenAmount),
                UpdateBlockHeight = lastUpdateHeight,
            }, GetDefaultEventContext(farmAddress, confirmStatus: confirmStatus));
        }
    }
}