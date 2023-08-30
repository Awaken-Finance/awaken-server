using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("LiquidateBorrow")]
    public class LiquidateBorrow : IEventDTO
    {
        [Parameter("address", "liquidator", 1, false)]
        public string Liquidator { get; set; }

        [Parameter("address", "borrower", 2, false)]
        public string Borrower { get; set; }

        [Parameter("uint256", "repayAmount", 3, false)]
        public BigInteger RepayAmount { get; set; }

        [Parameter("address", "gTokenCollateral", 4, false)]
        public string CTokenCollateral { get; set; }

        [Parameter("uint256", "seizeTokens", 5, false)]
        public BigInteger SeizeTokens { get; set; }
    }
}