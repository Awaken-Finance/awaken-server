using System;
using System.Numerics;
using Nethereum.Util;

namespace AwakenServer.Debits.Helpers
{
    public static class StatisticCalculationHelper
    {
        public static BigInteger EthMantissa { get; } = BigInteger.Pow(10, 18);
        public static int DaysPerYear { get; } = 365;
        public static double CalculateApy(BigDecimal speed, decimal compPrice, BigDecimal tokenValue, int blocksPerDay)
        {
            if (tokenValue == 0)
            {
                return 0;
            }

            return (double) (speed / tokenValue * compPrice * blocksPerDay * DaysPerYear);
        }
        
        public static double GetYearInterest(int blocksPerDay, BigDecimal borrowRate)
        {
            return Math.Pow((double) (borrowRate / EthMantissa * blocksPerDay) + 1, DaysPerYear);
        }
    }
}