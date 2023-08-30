using System.Numerics;

namespace AwakenServer.Debits
{
    public class DebitConstants
    {
        public static string Mantissa { get; } = BigInteger.Pow(10, 18).ToString();
        public const int DefaultSize = 100;
        public const int DefaultUserInfoSize = 100;
        public const int DefaultRecordSize = 50;
        public const string ZeroBalance = "0";
        // public const string ProjectTokenSymbol = "ISTAR";
    }
}