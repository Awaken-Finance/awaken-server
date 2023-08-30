using System.Numerics;

namespace AwakenServer.Debits.Ethereum
{
    public class DebitTestData
    {
        public const string DefaultNodeName = "default";
        public const int DefaultAElfChainId = 1;
        public const long CurrentBlockHeight = 10000;
        public const int BlockPerDay = 1000;
        
        public const string ProjectTokenContractAddress = "0xBCfCcbde45cE874adCB698cC183deBcF17952812";
        public const string ProjectTokenSymbol = "PROJECTTOKEN";
        public const int ProjectTokenDecimal = 18;
        
        public const string UsdtTokenContractAddress = "0xBCfCcbde45cE874adCB131gat83deBcF17952211";
        public const string UsdtTokenSymbol = "USDT";
        public const int UsdtTokenDecimal = 6;

        // CompController
        public const string ControllerAddress = "0xdCB698bde45cE874aC182deBcF17952813";
        public static string CloseFactorMantissa { get; } = (1 * BigInteger.Pow(10, 17)).ToString(); // 0.1
        
        // CToken
        public const string CTokenSymbol = "CPROJECTTOKEN";
        public const string CTokenAddress = "0xldFccbfqd341374adCB698cC182deBcF17098123";
        public const int CTokenDecimal = 18;
        public static string CollateralFactorMantissa { get; } = (9 * BigInteger.Pow(10, 17)).ToString(); // 0.9
        public static string ReserveFactorMantissa { get; } = (5 * BigInteger.Pow(10, 17)).ToString(); // 0.5

        public const string MintAction = "Mint";
        public const string BorrowAction = "Borrow";
        
        public const string UnderlyingTokenOneContractAddress = "0xBCfCcbde45cE874adCB698cC182deBcF17952813";
        public const string UnderlyingTokenOneSymbol = "PROJECTTOKENSWAP";
        public const int UnderlyingTokenOneDecimal = 18;
        
        public const string UnderlyingCTokenOneContractAddress = "0xl74adCB698cC182deBcF17098123deBcF1deBcF1";
        public const string UnderlyingCTokenOneSymbol = "WWL";
        public const int UnderlyingCTokenOneDecimal = 18;
        
        public const string ZeroBalance = "0";
        
        public const string Xi = "0xcdeqcbde54ca27rgdCn477cC182deBcF17952813";
        public const string Ming = "0xbc40cbde98ca20rgdCn928c2C182deBcF1712394";
        public const string Gui = "0xfxoplkqw92ca74rgdC3n491cC182deadf1098322";
    }
}