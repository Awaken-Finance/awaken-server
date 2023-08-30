using AElf.ContractTestKit;
using AElf.Types;
using AwakenServer.Tokens;

namespace AwakenServer.Dividend
{
    public class DividendTestConstants
    {
        public const int AElfChainId = 1;
        public const string MultiTokenAddress = "";
        public const string DividendTokenAddress = "DividendTokenAddress";
        public const string ElfTokenContractAddress = MultiTokenAddress;
        public const string ElfTokenSymbol = "ELF";
        public const int ElfTokenDecimal = 8;

        public const string ProjectTokenContractAddress = MultiTokenAddress;
        public const string ProjectTokenSymbol = "PROJECTTOKEN";
        public const int ProjectTokenDecimal = 8;

        public const string UsdtTokenContractAddress = MultiTokenAddress;
        public const string UsdtTokenSymbol = "USDT";
        public const int UsdtTokenDecimal = 6;

        public const long CurrentBlockHeight = 200;

        public static Address Qi { get; } = SampleAccount.Accounts[6].Address;
        public static Address Xue { get; } = SampleAccount.Accounts[7].Address;
        public static Address Ge { get; } = SampleAccount.Accounts[8].Address;
        public static Token[] Tokens { get; } = new[]
        {
            new Token
            {
                Address = ElfTokenContractAddress,
                Symbol = ElfTokenSymbol,
                Decimals = ElfTokenDecimal
            },
            new Token
            {
                Address = ProjectTokenContractAddress,
                Symbol = ProjectTokenSymbol,
                Decimals = ProjectTokenDecimal
            },
            new Token
            {
                Address = UsdtTokenContractAddress,
                Symbol = UsdtTokenSymbol,
                Decimals = UsdtTokenDecimal
            },
        };
    }
}