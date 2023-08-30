using System.Numerics;
using AElf.ContractTestKit;
using AElf.Types;

namespace AwakenServer.Debits.AElf
{
    public static class DebitTestData
    {
        public const string DefaultNodeName = "default";
        public const int DefaultAElfChainId = 1;
        public const long CurrentBlockHeight = 10000;
        public const int BlockPerDay = 1000;
        
        public static Address ProjectTokenContractAddress { get; } = SampleAccount.Accounts[0].Address;
        public const string ProjectTokenSymbol = "ProjectToken";
        public const int ProjectTokenDecimal = 18;
        
        public static Address UsdtTokenContractAddress { get; } = SampleAccount.Accounts[1].Address;
        public const string UsdtTokenSymbol = "USDT";
        public const int UsdtTokenDecimal = 6;

        // CompController
        public static Address ControllerAddress { get; } = SampleAccount.Accounts[2].Address;
        public static string CloseFactorMantissa { get; } = (1 * BigInteger.Pow(10, 17)).ToString(); // 0.1
        
        // CToken
        public static Address CTokenContractAddress { get; } = SampleAccount.Accounts[9].Address;
        public static string CTokenContractAddressStr { get; } = CTokenContractAddress.ToBase58();
        public const string CTokenSymbol = "CProjectToken";
        public static Address CTokenVirtualAddress { get; } = SampleAccount.Accounts[3].Address;
        public const int CTokenDecimal = 18;
        public static string CollateralFactorMantissa { get; } = (9 * BigInteger.Pow(10, 17)).ToString(); // 0.9
        public static string ReserveFactorMantissa { get; } = (5 * BigInteger.Pow(10, 17)).ToString(); // 0.5

        public const string MintAction = "Mint";
        public const string BorrowAction = "Borrow";
        
        public static Address UnderlyingTokenOneContractAddress { get; } = SampleAccount.Accounts[4].Address;
        public const string UnderlyingTokenOneSymbol = "PROJECTTOKENSWAP";
        public const int UnderlyingTokenOneDecimal = 18;
        
        public static Address UnderlyingCTokenOneContractAddress { get; } = SampleAccount.Accounts[5].Address;
        public const string UnderlyingCTokenOneSymbol = "WWL";
        public const int UnderlyingCTokenOneDecimal = 18;

        public const string ZeroBalance = "0";
        public static Address Xi { get; } = SampleAccount.Accounts[6].Address;
        public static Address Ming { get; }= SampleAccount.Accounts[7].Address;
        public static Address Gui { get; }= SampleAccount.Accounts[8].Address;
        public static Address UsdtCTokenVirtualAddress { get; } = SampleAccount.Accounts[9].Address;
    }
}