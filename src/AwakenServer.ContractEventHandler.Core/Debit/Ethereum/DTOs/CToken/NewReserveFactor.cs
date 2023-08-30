using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("NewReserveFactor")]
    public class NewReserveFactor : IEventDTO
    {
        [Parameter("uint256", "oldReserveFactorMantissa", 1, false)]
        public BigInteger OldReserveFactorMantissa { get; set; }

        [Parameter("uint256", "newReserveFactorMantissa", 2, false)]
        public BigInteger NewReserveFactorMantissa { get; set; }
    }
}