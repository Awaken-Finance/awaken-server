using AwakenServer.Tokens;

namespace AwakenServer.IDO
{
    public static class IDOTestConstants
    {
        public const int AElfChainId = 1;
        public const string MinedStatus = "Mined";
        public const string MultiTokenAddress = "";
        public const string ElfTokenContractAddress = MultiTokenAddress;
        public const string ElfTokenSymbol = "ELF";
        public const int ElfTokenDecimal = 8;
        
        public const string TokenOneContractAddress = MultiTokenAddress;
        public const string TokenOneSymbol = "YP";
        public const int TokenOneDecimal = 8;
        
        public const string TokenTwoContractAddress = MultiTokenAddress;
        public const string TokenTwoSymbol = "PY";
        public const int TokenTwoDecimal = 8;
        
        public const string TokenThreeContractAddress = MultiTokenAddress;
        public const string TokenThreeSymbol = "ZX";
        public const int TokenThreeDecimal = 16;
        
        public const string ProjectTokenContractAddress = MultiTokenAddress;
        public const string ProjectTokenSymbol = "PROJECTTOKEN";
        public const int ProjectTokenDecimal = 18;
        
        public const string UsdtTokenContractAddress = MultiTokenAddress;
        public const string UsdtTokenSymbol = "USDT";
        public const int UsdtTokenDecimal = 6;
        
        public static Token[] Tokens { get; }= new[]
        {
            new Token
            {
                Address = ElfTokenContractAddress,
                Symbol = ElfTokenSymbol,
                Decimals = ElfTokenDecimal
            },
            new Token
            {
                Address = TokenOneContractAddress,
                Symbol = TokenOneSymbol,
                Decimals = TokenOneDecimal
            },
            new Token
            {
                Address = TokenTwoContractAddress,
                Symbol = TokenTwoSymbol,
                Decimals = TokenTwoDecimal
            },
            new Token
            {
                Address = TokenThreeContractAddress,
                Symbol = TokenThreeSymbol,
                Decimals = TokenThreeDecimal
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