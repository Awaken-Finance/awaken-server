using System;
using Shouldly;
using Xunit;
using System.Numerics;
using AwakenServer.Farms.Helpers;

namespace AwakenServer.Farms.Ethereum.Tests
{
    public class FarmHelperTests : AwakenServerTestBase<AwakenServerApplicationTestModule>
    {
        // Massive 0
        // Compound 1
        [Theory(Skip = "no need")]
        [InlineData(1, 101, 1000, 1000, "500", "1000", 0, 0, 0, "0", 100, 100, 150, "0", "0")]
        [InlineData(1, 101, 1000, 1000, "500", "1000", 100, 0, 0, "0", 100, 0, 150, "0", "0")]
        [InlineData(1, 101, 1000, 1000, "500", "1000", 100, 0, 0, "0", 100, 50, 150, "0", "12500")]
        [InlineData(0, 101, 1000, 1000, "500", "10", 100, 101, 300, "100", 100, 50, 150, "2500",
            "12500")]
        public void Estimate_Pending_ProjectToken_Should_Be_Right(int farmType,
            long startBlock, long halvingPeriod1, long halvingPeriod2, string tokenPerBlock1Str, string tokenPerBlock2Str,
            int totalWeight, long usdtStartBlock, long usdtEndBlock, string usdtDividendPerBlockStr,
            long lastRewardBlock, int poolWeight,
            long currentBlockHeight, string pendingUsdtStr, string pendingTokenStr)
        {
            var tokenPerBlock1 = BigInteger.Parse(tokenPerBlock1Str);
            var tokenPerBlock2 = BigInteger.Parse(tokenPerBlock2Str);
            var usdtDividendPerBlock = BigInteger.Parse(usdtDividendPerBlockStr);
            var pendingUsdt = BigInteger.Parse(pendingUsdtStr);
            var pendingToken = BigInteger.Parse(pendingTokenStr);
            var (estimatePendingUsdt, estimatePendingToken) = ProjectTokenCalculationHelper.EstimatePendingRevenue(
                (FarmType) farmType,
                startBlock, halvingPeriod1, halvingPeriod2, tokenPerBlock1, tokenPerBlock2,
                totalWeight, usdtStartBlock, usdtEndBlock, usdtDividendPerBlock,
                lastRewardBlock, poolWeight,
                currentBlockHeight);
            estimatePendingUsdt.ShouldBe(pendingUsdt);
            estimatePendingToken.ShouldBe(pendingToken);
        }

        [Theory(Skip = "no need")]
        [InlineData(101, 150, "1000", 90, 100, "0")]
        [InlineData(101, 150, "1000", 90, 110, "10000")]
        [InlineData(101, 150, "1000", 110, 120, "10000")]
        [InlineData(101, 150, "1000", 140, 151, "10000")]
        [InlineData(101, 150, "1000", 151, 161, "0")]
        [InlineData(101, 150, "1000", 100, 151, "50000")]
        public void Estimate_Pending_Usdt_Should_Be_Right(long usdtStartBlock, long usdtEndBlock,
            string usdtPerBlockStr,
            long lastRewardBlock,
            long currentBlockHeight, string totalUsdtStr)
        {
            var usdtPerBlock = BigInteger.Parse(usdtPerBlockStr);
            var estimatePendingUsdt = ProjectTokenCalculationHelper.GetUsdtDividend(usdtStartBlock, usdtEndBlock, usdtPerBlock,
                lastRewardBlock,
                currentBlockHeight);
            estimatePendingUsdt.ShouldBe(BigInteger.Parse(totalUsdtStr));
        }

        [Theory(Skip = "no need")]
        [InlineData(100, 100, 120, 1000, "100", "2000")]
        [InlineData(100, 100, 120, 10, "100", "1500")]
        [InlineData(100, 100, 121, 10, "100", "1525")]
        public void GetCompoundTokenReward_Should_Be_Right(long startBlock, long fromBlock, long toBlock,
            long halvingPeriod,
            string tokenPerBlockStr, string reward)
        {
            var tokenPerBlock = BigInteger.Parse(tokenPerBlockStr);
            var tokenAmount = ProjectTokenCalculationHelper.GetCompoundProjectTokenReward(startBlock, fromBlock, toBlock,
                halvingPeriod,
                tokenPerBlock);
            tokenAmount.ShouldBe(BigInteger.Parse(reward));
        }

        [Theory(Skip = "no need")]
        [InlineData(100, 100, 120, 1000, 1000, "100", "100", "2000")]
        [InlineData(100, 200, 220, 100, 1000, "100", "100", "2000")]
        [InlineData(100, 100, 220, 100, 100, "100", "50", "11000")]
        [InlineData(100, 100, 220, 50, 50, "100", "200", "16000")]
        [InlineData(100, 100, 300, 50, 50, "100", "200", "22500")]
        [InlineData(100, 100, 301, 50, 50, "100", "200", "22525")]
        [InlineData(301, 100, 300, 50, 50, "100", "200", "0")]
        [InlineData(200, 100, 300, 50, 50, "100", "200", "25000")]
        [InlineData(100, 380, 700, 50, 50, "100", "200", "2850")]
        public void GetMassiveTokenReward_Should_Be_Right(long startBlock, long fromBlock, long toBlock,
            long halvingPeriod1,
            long halvingPeriod2, string tokenPerBlockStr1, string tokenPerBlockStr2, string reward)
        {
            var tokenPerBlock1 = BigInteger.Parse(tokenPerBlockStr1);
            var tokenPerBlock2 = BigInteger.Parse(tokenPerBlockStr2);
            var tokenAmount = ProjectTokenCalculationHelper.GetMassiveProjectTokenReward(startBlock, fromBlock, toBlock,
                halvingPeriod1, halvingPeriod2, tokenPerBlock1, tokenPerBlock2);
            tokenAmount.ShouldBe(BigInteger.Parse(reward));
        }

        [Theory(Skip = "no need")]
        [InlineData(100, 110, 0, 0, 0)]
        [InlineData(110, 100, 10, 10, 0)]
        [InlineData(100, 120, 10, 10, 0)]
        [InlineData(100, 121, 10, 10, 1)]
        public void MassivePhase_Should_Be_Right(long startBlock, long toBlock, long halvingPeriod1,
            long halvingPeriod2, int phase)
        {
            var actualPhase = ProjectTokenCalculationHelper.MassivePhase(startBlock, toBlock,
                halvingPeriod1, halvingPeriod2);
            actualPhase.ShouldBe(phase);
        }

        [Theory(Skip = "no need")]
        [InlineData(100, 110, 0, 0)]
        [InlineData(110, 100, 10, 0)]
        [InlineData(100, 120, 10, 1)]
        [InlineData(100, 121, 10, 2)]
        public void CompoundPhase_Should_Be_Right(long startBlock, long toBlock,
            long halvingPeriod, int phase)
        {
            var actualPhase = ProjectTokenCalculationHelper.CompoundPhase(startBlock, toBlock, halvingPeriod);
            actualPhase.ShouldBe(phase);
        }

        [Theory(Skip = "no need")]
        [InlineData(0, 8, "40000000000", 0, 40821, 86400, 259200, "10000000000", "5000000000", 1000, 300, 380000, 2365200)]
        [InlineData(1, 8, "40000000000", 0, 40821, 86400, 259200, "10000000000", "5000000000", 1000, 300, 470000, 2365200)]
        [InlineData(2, 8, "40000000000", 0, 40821, 86400, 259200, "10000000000", "5000000000", 1000, 300, 1400000, 295650)]
        [InlineData(3, 8, "40000000000", 0, 40821, 86400, 259200, "10000000000", "5000000000", 1000, 300, 1423230, 0)]
        [InlineData(0, 8, "99801111104", 1, 40821, 345600, 0, "5000000000", "0", 400, 300, 380000, 2369913)]
        [InlineData(0, 8, "99801111104", 1, 40821, 345600, 0, "5000000000", "0", 400, 300, 470000, 4923)]
        [InlineData(0, 8, "99801111104", 1, 40821, 345600, 0, "5000000000", "0", 400, 300, 1400000, 109)]
        [InlineData(1, 8, "12100000000", 1, 40821, 345600, 0, "5000000000", "0", 400, 100, 380000, 663)]
        [InlineData(1, 8, "12100000000", 1, 40821, 345600, 0, "5000000000", "0", 400, 100, 470000, 13534)]
        [InlineData(1, 8, "12100000000", 1, 40821, 345600, 0, "5000000000", "0", 400, 100, 1400000, 300)]
        [InlineData(0, 8, "99801111104", 1, 40821, 345600, 0, "5000000000", "0", 400, 300, 1423230, 0)]
        [InlineData(1, 8, "12100000000", 1, 40821, 345600, 0, "5000000000", "0", 400, 100, 1423230, 0)]
        public void GetPoolApy_Should_Be_Right(int pid, int poolTokenDecimal, string deposit, int farmType,
            long startBlock, long halvingPeriod1, long halvingPeriod2, string tokenPerBlock1Str, string tokenPerBlock2Str,
            int totalWeight,
            int poolWeight,
            long currentBlockHeight, decimal targetApy)
        {
            var tokenPerBlock1 = BigInteger.Parse(tokenPerBlock1Str);
            var tokenPerBlock2 = BigInteger.Parse(tokenPerBlock2Str);
           var apy = ProjectTokenCalculationHelper.CalculatePoolProjectTokenApyWithoutPrice(pid, poolTokenDecimal, deposit,
                (FarmType)farmType,
                startBlock, halvingPeriod1, halvingPeriod2, tokenPerBlock1, tokenPerBlock2,
                totalWeight,
                poolWeight,
                currentBlockHeight);
            decimal.Round(apy, 0).ShouldBe(targetApy);
        }

        [Fact(Skip = "no need")]
        public void EstimatePendingRevenue_Invalid_FarmType_Should_Throw_Exception()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ProjectTokenCalculationHelper.EstimatePendingRevenue((FarmType) 1000, 101, 1000, 1000,
                    BigInteger.Parse("500"), BigInteger.Parse("1000"), 100, 0, 0, BigInteger.Zero, 100, 50, 150);
            });
        }
    }
}