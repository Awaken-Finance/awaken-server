using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("Redeem")]
    public class Redeem : IEventDTO
    {
        [Parameter("address", "redeemer", 1, false)]
        public string Redeemer { get; set; }

        [Parameter("uint256", "redeemAmount", 2, false)]
        public BigInteger RedeemAmount { get; set; }

        [Parameter("uint256", "redeemTokens", 3, false)]
        public BigInteger RedeemTokens { get; set; }
    }
}