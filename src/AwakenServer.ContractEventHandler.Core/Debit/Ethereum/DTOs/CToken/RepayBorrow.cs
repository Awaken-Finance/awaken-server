using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("RepayBorrow")]
    public class RepayBorrow : IEventDTO
    {
        [Parameter("address", "payer", 1, false)]
        public string Payer { get; set; }

        [Parameter("address", "borrower", 2, false)]
        public string Borrower { get; set; }

        [Parameter("uint256", "repayAmount", 3, false)]
        public BigInteger RepayAmount { get; set; }

        [Parameter("uint256", "accountBorrows", 4, false)]
        public BigInteger AccountBorrows { get; set; }

        [Parameter("uint256", "accountBorrowIndex", 5, false)]
        public BigInteger AccountBorrowIndex { get; set; }

        [Parameter("uint256", "totalBorrows", 6, false)]
        public BigInteger TotalBorrows { get; set; }
    }
}