using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("Borrow")]
    public class Borrow : IEventDTO
    {
        [Parameter("address", "borrower", 1, false)]
        public string Borrower { get; set; }

        [Parameter("uint256", "borrowAmount", 2, false)]
        public BigInteger BorrowAmount { get; set; }

        [Parameter("uint256", "accountBorrows", 3, false)]
        public BigInteger AccountBorrows { get; set; }

        [Parameter("uint256", "accountBorrowIndex", 4, false)]
        public BigInteger AccountBorrowIndex { get; set; }

        [Parameter("uint256", "totalBorrows", 5, false)]
        public BigInteger TotalBorrows { get; set; }

        [Parameter("string", "channel", 6, false)]
        public string Channel { get; set; }
    }
}