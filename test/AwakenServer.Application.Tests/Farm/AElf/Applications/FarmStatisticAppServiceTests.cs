using System.Threading.Tasks;
using AElf.ContractTestKit;
using AwakenServer.Farm;
using Shouldly;
using Xunit;

namespace AwakenServer.Farms.AElf.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task GetPoolsStatisticInfo_Result_Should_Be_Right()
        {
            var user1 = SampleAccount.Accounts[0].Address;
            var user2 = SampleAccount.Accounts[1].Address;
            await CreateRecordData(user1, user2);
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 2;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenTwoSymbol;
            var lastRewardBlock = 1230;
            var weight = 1000;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);
            var poolStatisticInfo =
                await _farmStatisticAppService.GetPoolsStatisticInfo(new GetPoolsTotalStatisticInput
                {
                    ChainId = DefaultChainId
                });
            poolStatisticInfo.TotalDepositValue.ShouldBeGreaterThan(10m);
            
            poolStatisticInfo =
                await _farmStatisticAppService.GetPoolsStatisticInfo(new GetPoolsTotalStatisticInput());
            poolStatisticInfo.TotalDepositValue.ShouldBeGreaterThan(10m);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetUsersStatisticInfo_Result_Should_Be_Right()
        {
            var user1 = SampleAccount.Accounts[0].Address;
            var user2 = SampleAccount.Accounts[1].Address;
            await CreateRecordData(user1, user2);
            var userStatisticInfo =
                await _farmStatisticAppService.GetUsersStatisticInfo(new GetUsersTotalStatisticInput
                {
                    ChainId = DefaultChainId,
                    User = user2.ToBase58()
                });
            userStatisticInfo.TotalDepositBtcValue.ShouldBeGreaterThan(0);
            userStatisticInfo.TotalDepositUsdtValue.ShouldBeGreaterThan(0);
            userStatisticInfo.TotalRevenueUsdtValue.ShouldBe(0);
        }
    }
}