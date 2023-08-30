using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("ReservesReduced")]
    public class ReservesReduced : IEventDTO
    {
        [Parameter("address", "admin", 1, false)]
        public string Admin { get; set; }

        [Parameter("uint256", "reduceAmount", 2, false)]
        public BigInteger ReduceAmount { get; set; }

        [Parameter("uint256", "newTotalReserves", 3, false)]
        public BigInteger NewTotalReserves { get; set; }
    }
}