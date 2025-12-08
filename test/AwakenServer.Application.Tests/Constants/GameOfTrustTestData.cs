using System;
using System.Numerics;
using Nethereum.Util;

namespace AwakenServer.Constants
{
    public class GameOfTrustTestData
    {
        public const string DefaultNodeName = "Ethereum";

        public const string ContractAddress = "0x902F1767e142B27e9a068d00aAA8eA0D820B359A";
        public const string ContractAddressSashimi = "0x902F1767e142B27e9a068d00aAA8eA0D820B359B";
        
        public const string DepositTokenSymbol01 = "Sashimi Token";
        public const string DepositTokenSymbol02 = "Project Token";
        public const string HarvestTokenSymbol = "Project Token";
        public const string AnchorTokenSymbol = "USDT";

        public const int WaitEsDelayMs = 1000;

        // pool-0
        public const long BlocksDaily = 8749;
        public static string ChainId = Guid.Parse("39ffd932-2755-0358-93ed-8e3049963754").ToString();
        public static long EndHeight=2800000;
        public static long StartHeight = 2000000;
        public static long UnlockCycle = 10000;
        public static BigDecimal FineAmount = BigDecimal.Parse("10000");
        public static BigInteger RewardRate = 500;
        public static BigDecimal TotalAmountLimit = BigDecimal.Parse("100000000");
        public static BigDecimal TotalValueLocked = 0;
        public static BigDecimal UnlockMarketCap = BigDecimal.Parse("10000000000");
        
        // user1
        public static String ADDRESS_USER1 = "0x9ed836883d1f2A08b1f6Da7e2d7F7f93272FED80";
        public static String ADDRESS_USER2 = "0x1d5F9cb0d5ac49fd67765Ea1C6168725dF433DC4";
        public static String ADDRESS_USER3 = "0x079A713d9E3E2336ab0c893A733FFd70c806B074";
        
        
    }
}