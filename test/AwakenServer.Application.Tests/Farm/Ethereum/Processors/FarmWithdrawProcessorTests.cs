using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs;
using AwakenServer.Farm;
using Shouldly;
using Xunit;

namespace AwakenServer.Farms.Ethereum.Tests
{
    public partial class FarmAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Withdraw_Should_Add_User_And_Record()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);

            var depositAmount = 999999900;
            var depositTxHash = "depositone";
            var depositTimestamp = DateTime.Now;
            var user = FarmTestData.Wei;
            await DepositAsync(user, farmAddress, pid, depositTxHash, depositTimestamp, depositAmount);

            var withdrawTxHash = "withdrawone";
            var withdrawAmount = 999999900;
            var withdrawTimestamp = depositTimestamp.AddHours(1);
            await WithdrawAsync(user, farmAddress, pid, withdrawTxHash, withdrawAmount, withdrawTimestamp,
                ContractEventStatus.Unconfirmed);
            var userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == FarmTestData.MassiveFarmAddress);
            userInfo.CurrentDepositAmount.ShouldNotBe(FarmTestData.ZeroBalance);

            await WithdrawAsync(user, farmAddress, pid, withdrawTxHash, withdrawAmount, withdrawTimestamp);
            userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == FarmTestData.MassiveFarmAddress);
            userInfo.CurrentDepositAmount.ShouldBe(FarmTestData.ZeroBalance);

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.TotalDepositAmount.ShouldBe(FarmTestData.ZeroBalance);

            var records = (await _esRecordRepository.GetListAsync()).Item2;
            var targetRecord = records.First(x =>
                DateTimeHelper.ToUnixTimeMilliseconds(x.Date) / 1000 ==
                DateTimeHelper.ToUnixTimeMilliseconds(withdrawTimestamp) / 1000
                && userInfo.User == user &&
                x.BehaviorType == BehaviorType.Withdraw);
            targetRecord.Amount.ShouldBe(withdrawAmount.ToString());
            targetRecord.FarmInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.PoolInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.TokenInfo.Id.ShouldNotBe(Guid.Empty);
        }

        private async Task WithdrawAsync(string user, string farmAddress, int pid, string txHash, long amount,
            DateTime date, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var withdrawProcessor = GetRequiredService<IEventHandlerTestProcessor<Withdraw>>();
            await withdrawProcessor.HandleEventAsync(new Withdraw
            {
                User = user,
                Amount = new BigInteger(amount),
                Pid = pid
            }, GetDefaultEventContext(farmAddress, txHash, timestamp, confirmStatus));
        }
    }
}