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
        public async Task Deposit_Should_Add_User_And_Record()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 1001;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            var depositAmount = 999999900;
            var depositTxHash = "depositone";
            var depositTimestamp = DateTime.Now;
            var user = FarmTestData.Wei;
            await DepositAsync(user, farmAddress, pid, depositTxHash, depositTimestamp, depositAmount,
                ContractEventStatus.Unconfirmed);
            var userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.FirstOrDefault(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == farmAddress);
            userInfo.ShouldBeNull();

            await DepositAsync(user, farmAddress, pid, depositTxHash, depositTimestamp, 0);
            userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.FirstOrDefault(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == farmAddress);
            userInfo.ShouldBeNull();

            await DepositAsync(user, farmAddress, pid, depositTxHash, depositTimestamp, depositAmount);
            userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == farmAddress);
            userInfo.Id.ShouldNotBe(Guid.Empty);
            userInfo.FarmInfo.Id.ShouldNotBe(Guid.Empty);
            userInfo.PoolInfo.Id.ShouldNotBe(Guid.Empty);
            userInfo.SwapToken.Id.ShouldNotBe(Guid.Empty);
            userInfo.User.ShouldBe(user);
            userInfo.CurrentDepositAmount.ShouldBe(depositAmount.ToString());

            var (_, pools) = await _esPoolRepository.GetListAsync();
            var targetPool = pools.First(x => x.Pid == pid);
            targetPool.TotalDepositAmount.ShouldBe(depositAmount.ToString());

            var records = (await _esRecordRepository.GetListAsync()).Item2;
            var targetRecord = records.First(x =>
                DateTimeHelper.ToUnixTimeMilliseconds(x.Date) / 1000 ==
                DateTimeHelper.ToUnixTimeMilliseconds(depositTimestamp) / 1000 && userInfo.User == user &&
                x.BehaviorType == BehaviorType.Deposit);
            targetRecord.Amount.ShouldBe(depositAmount.ToString());
            targetRecord.FarmInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.FarmInfo.ChainId.ShouldNotBeNull();
            targetRecord.PoolInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.PoolInfo.ChainId.ShouldNotBeNull();
            targetRecord.TokenInfo.Id.ShouldNotBe(Guid.Empty);
        }

        [Fact(Skip = "no need")]
        public async Task Deposit_Repeat_Should_Not_Add_User()
        {
            var farmAddress = FarmTestData.MassiveFarmAddress;
            var pid = 0;
            var poolType = PoolType.Massive;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 1001;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            var depositAmount = 999999900;
            var depositTxHash = "depositone";
            var depositTimestamp = DateTime.Now;
            var user = FarmTestData.Wei;
            await DepositAsync(user, farmAddress, pid, depositTxHash, depositTimestamp, depositAmount);
            var userCount = (await _esFarmUserInfoRepository.GetListAsync()).Item1;
            userCount.ShouldBe(1);
            var recordCount = (await _esRecordRepository.GetListAsync()).Item1;
            recordCount.ShouldBe(1);

            await DepositAsync(user, farmAddress, pid, depositTxHash, depositTimestamp, depositAmount);
            userCount = (await _esFarmUserInfoRepository.GetListAsync()).Item1;
            userCount.ShouldBe(1);
            recordCount = (await _esRecordRepository.GetListAsync()).Item1;
            recordCount.ShouldBe(2);
        }

        private async Task DepositAsync(string user, string farmAddress, int pid, string txHash,
            DateTime date,
            long depositAmount,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var depositProcessor = GetRequiredService<IEventHandlerTestProcessor<Deposit>>();
            await depositProcessor.HandleEventAsync(new Deposit
            {
                User = user,
                Amount = new BigInteger(depositAmount),
                Pid = pid
            }, GetDefaultEventContext(farmAddress, txHash, timestamp, confirmStatus));
        }
    }
}