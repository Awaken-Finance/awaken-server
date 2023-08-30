using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using Xunit;
using AElf.ContractTestKit;
using AElf.Types;
using Awaken.Contracts.Farm;
using AwakenServer.Farm;
using Shouldly;

namespace AwakenServer.Farms.AElf.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Massive_Claim_Should_Add_User_ProjectToken_And_Record()
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

            var claimAmount = 10099;
            var txHash = "claimone";
            var currentTimestamp = depositTimestamp.AddHours(1);
            await MassiveClaimAsync(user, DividendTokenType.ProjectToken, farmAddress, pid, txHash, claimAmount,
                currentTimestamp);
            var userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == farmAddress);
            userInfo.AccumulativeDividendProjectTokenAmount.ShouldBe(claimAmount.ToString());

            var records = (await _esRecordRepository.GetListAsync()).Item2;
            var targetRecord = records.First(x =>
                DateTimeHelper.ToUnixTimeMilliseconds(x.Date) ==
                DateTimeHelper.ToUnixTimeMilliseconds(currentTimestamp) && userInfo.User == user.ToBase58() &&
                x.BehaviorType == BehaviorType.ClaimDistributedToken && x.TokenInfo.Symbol == tokenSymbol);
            targetRecord.Amount.ShouldBe(claimAmount.ToString());
        }

        [Fact(Skip = "no need")]
        public async Task Massive_Claim_Should_Add_User_Usdt_And_Record()
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

            var claimAmount = 10099;
            var txHash = "claimTwo";
            var currentTimestamp = depositTimestamp.AddHours(1);
            await MassiveClaimAsync(user, DividendTokenType.USDT, farmAddress, pid, txHash, claimAmount,
                currentTimestamp);

            var userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == farmAddress);
            userInfo.AccumulativeDividendUsdtAmount.ShouldBe(claimAmount.ToString());
            userInfo.AccumulativeDividendProjectTokenAmount.ShouldBe("0");

            var records = (await _esRecordRepository.GetListAsync()).Item2;
            var targetRecord = records.First(x =>
                DateTimeHelper.ToUnixTimeMilliseconds(x.Date) ==
                DateTimeHelper.ToUnixTimeMilliseconds(currentTimestamp) && userInfo.User == user.ToBase58() &&
                x.BehaviorType == BehaviorType.ClaimUsdt && x.TokenInfo.Symbol == tokenSymbol);
            targetRecord.Amount.ShouldBe(claimAmount.ToString());
        }

        private async Task MassiveClaimAsync(Address user, DividendTokenType tokenType, string farmAddress, int pid, string txHash,
            long amount, DateTime date)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var claimProcessor = GetRequiredService<IEventHandlerTestProcessor<ClaimRevenue>>();
            await claimProcessor.HandleEventAsync(new ClaimRevenue
            {
                User = user,
                Amount = amount,
                Pid = pid,
                TokenType = (int)tokenType
            }, GetDefaultEventContext(farmAddress, txHash, timestamp));
        }
    }
}