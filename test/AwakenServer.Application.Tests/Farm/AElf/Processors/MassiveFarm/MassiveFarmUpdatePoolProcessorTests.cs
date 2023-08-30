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
        public async Task MassiveFarm_Pool_Update_Should_Modify_ProjectToken()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(FarmTestData.ZeroBalance);

            var tokenAmount = 1000213;
            var usdtAmount = 123423;
            long lastUpdateHeight = 1000999;

            await UpdateMassiveFarmPool(farmAddress, pid, tokenAmount, usdtAmount, lastUpdateHeight);
            (_, pools) = await _esPoolRepository.GetListAsync();
            targetPool = pools.First(x => x.Pid == pid);
            targetPool.AccumulativeDividendProjectToken.ShouldBe(tokenAmount.ToString());
            targetPool.AccumulativeDividendUsdt.ShouldBe(usdtAmount.ToString());
        }

        [Fact(Skip = "no need")]
        public async Task MassiveFarm_Pool_Update_Twice_Should_Update_Latest_Height()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);
            
            var tokenAmount = 1000213;
            var usdtAmount = 123423;
            long lastUpdateHeight = 1000999;
            long smallerHeight = 123;
            await UpdateMassiveFarmPool(farmAddress, pid, tokenAmount, usdtAmount, lastUpdateHeight);
            await UpdateMassiveFarmPool(farmAddress, pid, tokenAmount, usdtAmount, smallerHeight);
            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.LastUpdateBlockHeight.ShouldBe(lastUpdateHeight);
        }

        private async Task UpdateMassiveFarmPool(string farmAddress, int pid, long tokenAmount, long usdtAmount,
            long lastUpdateHeight)
        {
            var updatePoolProcessor = GetRequiredService<IEventHandlerTestProcessor<UpdatePool>>();
            await updatePoolProcessor.HandleEventAsync(new UpdatePool
            {
                Pid = pid,
                DistributeTokenAmount = tokenAmount,
                UpdateBlockHeight = lastUpdateHeight,
                UsdtAmount = usdtAmount
            }, GetDefaultEventContext(farmAddress));
        }
    }
}