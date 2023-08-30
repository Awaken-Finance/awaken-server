using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("PlatformTokenSpeedUpdated")]
    public class CompBorrowSpeedUpdated : IEventDTO
    {
        [Parameter("address", "gToken", 1, true)]
        public string CToken { get; set; }

        [Parameter("uint256", "newSpeed", 2, false)]
        public BigInteger NewSpeed { get; set; }
    }
}