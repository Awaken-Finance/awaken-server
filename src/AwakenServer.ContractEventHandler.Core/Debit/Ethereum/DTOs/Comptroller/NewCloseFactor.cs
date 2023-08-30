using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("NewCloseFactor")]
    public class NewCloseFactor : IEventDTO
    {
        [Parameter("uint256", "oldCloseFactorMantissa", 1, false)]
        public BigInteger OldCloseFactorMantissa { get; set; }

        [Parameter("uint256", "newCollateralFactorMantissa", 2, false)]
        public BigInteger NewCloseFactorMantissa { get; set; }
    }
}