using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.Farm;
using Xunit;
using AElf.ContractTestKit;
using AwakenServer.Farm;
using Shouldly;

namespace AwakenServer.Farms.AElf.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Withdraw_Should_Add_User_And_Record()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenSymbol = FarmTestData.SwapTokenOneSymbol;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenSymbol, lastRewardBlock, weight);

            var depositAmount = 999999900;
            var depositTxHash = "depositone";
            var depositTimestamp = DateTime.Now;
            var user = SampleAccount.Accounts[0].Address;
            await DepositAsync(user, farmAddress, pid, depositTxHash, depositTimestamp, depositAmount);

            var withdrawTxHash = "withdrawone";
            var withdrawAmount = 999999900;
            var withdrawTimestamp = depositTimestamp.AddHours(1);
            await WithdrawAsync(user, farmAddress, pid, withdrawTxHash, withdrawAmount, withdrawTimestamp);
            var userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == FarmTestData.MassiveFarmAddress);
            userInfo.CurrentDepositAmount.ShouldBe(FarmTestData.ZeroBalance);

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);

            var records = (await _esRecordRepository.GetListAsync()).Item2;
            var targetRecord = records.First(x =>
                DateTimeHelper.ToUnixTimeMilliseconds(x.Date) / 1000 ==
                DateTimeHelper.ToUnixTimeMilliseconds(withdrawTimestamp) / 1000
                && userInfo.User == user.ToBase58() &&
                x.BehaviorType == BehaviorType.Withdraw);
            targetRecord.Amount.ShouldBe(withdrawAmount.ToString());
            targetRecord.FarmInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.PoolInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.TokenInfo.Id.ShouldNotBe(Guid.Empty);
        }

        private async Task WithdrawAsync(Address user, string farmAddress, int pid, string txHash, long amount,
            DateTime date)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var withdrawProcessor = GetRequiredService<IEventHandlerTestProcessor<Withdraw>>();
            await withdrawProcessor.HandleEventAsync(new Withdraw
            {
                User = user,
                Amount = amount,
                Pid = pid
            }, GetDefaultEventContext(farmAddress, txHash, timestamp));
        }
    }
}