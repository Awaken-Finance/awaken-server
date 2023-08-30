using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.Constants;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Applications.GameOfTrust
{
    public partial class GameOfTrustAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Update_Pool_Should_Success()
        {
            await initPool();
            var contractAddress = GameOfTrustTestData.ContractAddress;
            var pid = 0;
            var marketCap = BigInteger.Parse("200") * BigInteger.Pow(10, 18);
            var rewardRate = 550;
            var totalAmountLimit = BigInteger.Parse("2")*BigInteger.Pow(10,18);
            var unlockCycle = 20000;
            await UpdatePoolAsync(contractAddress, pid, marketCap, rewardRate, totalAmountLimit, unlockCycle);
            var (_, gameList) = await _esGameRepository.GetListAsync();
            var gameOfTrust = gameList.First(x => x.Address == contractAddress && x.Pid == pid);
            gameOfTrust.Pid.ShouldBe(pid);
            var finalMarketCap = BigDecimal.Parse(marketCap.ToString()) / BigInteger.Pow(10, tokenUSD.Decimals);
            gameOfTrust.UnlockMarketCap.ShouldBe(finalMarketCap.ToString());
            gameOfTrust.RewardRate.ShouldBe(rewardRate.ToString());
            var finalTotalAmountLimit = (BigDecimal) totalAmountLimit / BigInteger.Pow(10, tokenA.Decimals);
            gameOfTrust.TotalAmountLimit.ShouldBe(finalTotalAmountLimit.ToString());
            gameOfTrust.UnlockCycle.ShouldBe(unlockCycle);
            gameOfTrust.Pid.ShouldBe(pid);
        }

        private async Task UpdatePoolAsync(
            string contractAddress,
            int pid,
            BigInteger marketCap,
            long rewardRate,
            BigInteger totalAmountLimit,
            long unlockCycle)
        {
            var updatePoolProcessor = GetRequiredService<IEventHandlerTestProcessor<UpdatePoolEventDto>>();
            await updatePoolProcessor.HandleEventAsync(new UpdatePoolEventDto
            {
                Pid = pid,
                MarketCap = marketCap,
                RewardRate = int.Parse(rewardRate.ToString()),
                UnlockCycle = unlockCycle,
                TotalAmountLimit = totalAmountLimit
            }, GetDefaultEventContext(contractAddress));
        }
    }
}