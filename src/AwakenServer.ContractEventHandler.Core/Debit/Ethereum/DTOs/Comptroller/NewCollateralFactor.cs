using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("NewCollateralFactor")]
    public class NewCollateralFactor : IEventDTO
    {
        [Parameter("address", "gToken", 1, false)]
        public string CToken { get; set; }

        [Parameter("uint256", "oldCollateralFactorMantissa", 2, false)]
        public BigInteger OldCollateralFactorMantissa { get; set; }

        [Parameter("uint256", "newCollateralFactorMantissa", 3, false)]
        public BigInteger NewCollateralFactorMantissa { get; set; }
    }
}