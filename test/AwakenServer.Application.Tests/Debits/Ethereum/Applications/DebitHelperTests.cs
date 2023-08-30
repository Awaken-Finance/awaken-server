using AwakenServer.Debits.Helpers;
using Nethereum.Util;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public class DebitHelperTests : AwakenServerTestBase<AwakenServerApplicationTestModule>
    {
        // todo modify interest model test
        // [Theory]
        // [InlineData(100, 101, "1000", "1000", "500", 0, 0)]
        // [InlineData(0, 100, "1000", "1000", "1000", 1, 1)]
        // [InlineData(0, 100, "1000", "0", "1000", 0, 0)]
        // [InlineData(0, 100, "1000", "1000", "2000", 0, 0)]
        // public void YearInterestCalculateService_Test(int interestType, int blocksPerDay, string cashStr,
        //     string borrowsStr, string reservesStr, int targetBorrowRate,
        //     int targetSupplyRate)
        // {
        //     var cash = BigInteger.Parse(cashStr);
        //     var borrows = BigInteger.Parse(borrowsStr);
        //     var reserves = BigInteger.Parse(reservesStr);
        //     var reserveFactorMantissa = BigInteger.Parse(DebitTestData.ReserveFactorMantissa);
        //     var tokenRateCalculateService = GetRequiredService<ITokenRateCalculateService>();
        //     var borrowRate =
        //         tokenRateCalculateService.GetBorrowRate((InterestModelType) interestType, cash, borrows, reserves);
        //     var supplyRate = tokenRateCalculateService.GetSupplyRate((InterestModelType) interestType, cash, borrows,
        //         reserves, reserveFactorMantissa);
        //     var borrowYearInterest = StatisticCalculationHelper.GetYearInterest(blocksPerDay, borrowRate);
        //     var supplyYearInterest = StatisticCalculationHelper.GetYearInterest(blocksPerDay, supplyRate);
        //     borrowYearInterest.ShouldBeGreaterThanOrEqualTo(targetBorrowRate);
        //     supplyYearInterest.ShouldBeGreaterThanOrEqualTo(targetSupplyRate);
        // }

        [Theory(Skip = "no need")]
        [InlineData("1000", 101, "0", 1000, 0)]
        [InlineData("1000", 100, "1000000", 100, 3650)]
        public void CalculateApy_Test(string speed, decimal compPrice, string tokenValueStr, int blocksPerDay,
            int expectApy)
        {
            var tokenValue = BigDecimal.Parse(tokenValueStr);
            var apy = StatisticCalculationHelper.CalculateApy(BigDecimal.Parse(speed), compPrice, tokenValue,
                blocksPerDay);
            apy.ShouldBeGreaterThanOrEqualTo(expectApy);
        }
    }
}