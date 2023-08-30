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
        public async Task GeneralFarm_Pool_Update_Should_Modify_ProjectToken_And_Usdt()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var pid = 0;
            var poolType = PoolType.Normal;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);

            var ProjectAmount = 1000213;
            long lastUpdateHeight = 1000999;
            await UpdateGeneralFarmPool(farmAddress, pid, ProjectAmount, lastUpdateHeight);
            (_, pools) = await _esPoolRepository.GetListAsync();
            targetPool = pools.First(x => x.Pid == pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(ProjectAmount.ToString());
        }

        [Fact(Skip = "no need")]
        public async Task GeneralFarm_Pool_Update_Twice_Should_Update_Latest_Height()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var pid = 0;
            var poolType = PoolType.Normal;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);
            
            var tokenAmount = 1000213;
            long lastUpdateHeight = 1000999;
            long smallerHeight = 213;
            await UpdateGeneralFarmPool(farmAddress, pid, tokenAmount, lastUpdateHeight);
            await UpdateGeneralFarmPool(farmAddress, pid, tokenAmount, smallerHeight);
            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastUpdateHeight);
        }

        private async Task UpdateGeneralFarmPool(string farmAddress, int pid, long tokenAmount, long lastUpdateHeight)
        {
            var updatePoolProcessor = GetRequiredService<IEventHandlerTestProcessor<UpdatePool>>();
            await updatePoolProcessor.HandleEventAsync(new UpdatePool
            {
                Pid = pid,
                DistributeTokenAmount = tokenAmount,
                UpdateBlockHeight = lastUpdateHeight
            }, GetDefaultEventContext(farmAddress));
        }
    }
}