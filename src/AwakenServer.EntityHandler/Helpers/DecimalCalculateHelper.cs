using System.Numerics;

namespace AwakenServer.EntityHandler.Helpers
{
    public static class DecimalCalculateHelper
    {
        public static double GetDecimalAmount(BigInteger amount, int deci)
        {
            if (double.TryParse(amount.ToString(), out var accurate))
            {
                return accurate / deci;
            }

            amount /= deci;
            return double.TryParse(amount.ToString(), out accurate) ? accurate : 0;
        }
        
        public static double GetDecimalAmount(string amount, int deci)
        {
            if (double.TryParse(amount, out var accurate))
            {
                return accurate / deci;
            }
            
            amount = (BigInteger.Parse(amount)/deci).ToString();
            return double.TryParse(amount, out accurate) ? accurate : 0; 
        }
    }
}