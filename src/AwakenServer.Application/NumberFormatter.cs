using System;
using Nethereum.Util;

namespace AwakenServer
{
    public static class NumberFormatter
    {
        public static string ToDecimalsString(this long number, int decimals)
        {
            var num = number / Math.Pow(10, decimals);
            return new BigDecimal(num).ToNormalizeString();
        }
        
        public static string ToNormalizeString(this BigDecimal bigDecimal)
        {
            if (bigDecimal >= 0)
            {
                return bigDecimal.ToString();
            }

            var value = -bigDecimal;
            return "-" + value;
        }
    }
}