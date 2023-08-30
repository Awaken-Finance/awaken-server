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
        public async Task Claim_Should_Add_User_Token_And_Record()
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

            var claimAmount = 10099;
            var txHash = "claimone";
            var currentTimestamp = depositTimestamp.AddHours(1);
            var dividendTokenAddress = FarmTestData.ProjectTokenContractAddress;

            await ClaimAsync(user, farmAddress, dividendTokenAddress, DividendTokenType.ProjectToken, pid, txHash, claimAmount,
                currentTimestamp, ContractEventStatus.Unconfirmed);
            var records = (await _esRecordRepository.GetListAsync()).Item2;
            var targetRecord = records.FirstOrDefault(x => x.BehaviorType == BehaviorType.ClaimDistributedToken);
            targetRecord.ShouldBeNull();

            await ClaimAsync(user, farmAddress, dividendTokenAddress, DividendTokenType.ProjectToken, pid, txHash, claimAmount,
                currentTimestamp);
            var userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == farmAddress);
            userInfo.AccumulativeDividendProjectTokenAmount.ShouldBe(claimAmount.ToString());

            records = (await _esRecordRepository.GetListAsync()).Item2;
            targetRecord = records.First(x =>
                DateTimeHelper.ToUnixTimeMilliseconds(x.Date) / 1000 ==
                DateTimeHelper.ToUnixTimeMilliseconds(currentTimestamp) / 1000 && userInfo.User == user &&
                x.BehaviorType == BehaviorType.ClaimDistributedToken && x.TokenInfo.Address == tokenAddress);
            targetRecord.Amount.ShouldBe(claimAmount.ToString());
        }

        [Fact(Skip = "no need")]
        public async Task Claim_Should_Add_User_Usdt_And_Record()
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

            var claimAmount = 10099;
            var txHash = "claimTwo";
            var currentTimestamp = depositTimestamp.AddHours(1);
            var dividendTokenAddress = FarmTestData.UsdtTokenContractAddress;
            await ClaimAsync(user, farmAddress, dividendTokenAddress, DividendTokenType.USDT, pid, txHash, claimAmount,
                currentTimestamp);

            var userInfo = (await _esFarmUserInfoRepository.GetListAsync()).Item2.First(x =>
                x.PoolInfo.Pid == pid && x.FarmInfo.FarmAddress == farmAddress);
            userInfo.AccumulativeDividendUsdtAmount.ShouldBe(claimAmount.ToString());

            var records = (await _esRecordRepository.GetListAsync()).Item2;
            var targetRecord = records.First(x =>
                DateTimeHelper.ToUnixTimeMilliseconds(x.Date) / 1000 ==
                DateTimeHelper.ToUnixTimeMilliseconds(currentTimestamp) / 1000 && userInfo.User == user &&
                x.BehaviorType == BehaviorType.ClaimUsdt && x.TokenInfo.Address == tokenAddress);
            targetRecord.Amount.ShouldBe(claimAmount.ToString());
        }

        private async Task ClaimAsync(string user, string farmAddress, string tokenAddress,
            DividendTokenType tokenType, int pid, string txHash,
            long amount, DateTime date, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var claimProcessor = GetRequiredService<IEventHandlerTestProcessor<ClaimRevenue>>();
            await claimProcessor.HandleEventAsync(new ClaimRevenue
            {
                User = user,
                Amount = new BigInteger(amount),
                Pid = pid,
                TokenAddress = tokenAddress,
                DividendTokenType = tokenType
            }, GetDefaultEventContext(farmAddress, txHash, timestamp, confirmStatus));
        }
    }
}