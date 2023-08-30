using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("DistributedBorrowerPlatformToken")]
    public class DistributedBorrowerComp : IEventDTO
    {
        [Parameter("address", "gToken", 1, true)]
        public string CToken { get; set; }

        [Parameter("address", "borrower", 2, true)]
        public string Borrower { get; set; }

        [Parameter("uint256", "platformTokenDelta", 3, false)]
        public BigInteger CompDelta { get; set; }

        [Parameter("uint256", "platformTokenBorrowIndex", 4, false)]
        public BigInteger CompBorrowIndex { get; set; }
    }
}