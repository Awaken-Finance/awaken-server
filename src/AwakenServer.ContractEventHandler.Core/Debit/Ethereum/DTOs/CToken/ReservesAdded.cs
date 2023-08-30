using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("ReservesAdded")]
    public class ReservesAdded : IEventDTO
    {
        [Parameter("address", "benefactor", 1, false)]
        public string Benefactor { get; set; }

        [Parameter("uint256", "addAmount", 2, false)]
        public BigInteger AddAmount { get; set; }

        [Parameter("uint256", "newTotalReserves", 3, false)]
        public BigInteger NewTotalReserves { get; set; }
    }
}