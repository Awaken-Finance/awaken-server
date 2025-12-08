using System.Numerics;
using Nethereum.Util;

namespace AwakenServer.EntityHandler.Helpers
{
    public static class CalculationHelper
    {
        public static double GetDecimalAmount(string amount, int deci)
        {
            return (double)(BigDecimal.Parse(amount) / BigInteger.Pow(10, deci));
        }
    }
}