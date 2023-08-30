using System.Numerics;

namespace AwakenServer.ContractEventHandler.Helpers
{
    public static class CalculationHelper
    {
        public static string Add(string originAmount, BigInteger amount)
        {
            return (BigInteger.Parse(originAmount) + amount).ToString();
        }
        
        public static string Minus(string originAmount, BigInteger amount)
        {
            return (BigInteger.Parse(originAmount) - amount).ToString();
        }
        
        public static string Add(string originAmount, string amount)
        {
            return (BigInteger.Parse(originAmount) + BigInteger.Parse(amount)).ToString();
        }
        
        public static string Minus(string originAmount, string amount)
        {
            return (BigInteger.Parse(originAmount) - BigInteger.Parse(amount)).ToString();
        }
    }
}