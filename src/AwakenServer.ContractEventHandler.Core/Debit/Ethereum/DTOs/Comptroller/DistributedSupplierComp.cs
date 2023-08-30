using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("DistributedSupplierPlatformToken")]
    public class DistributedSupplierComp : IEventDTO
    {
        [Parameter("address", "gToken", 1, true)]
        public string CToken { get; set; }

        [Parameter("address", "supplier", 2, true)]
        public string Supplier { get; set; }

        [Parameter("uint256", "platformTokenDelta", 3, false)]
        public BigInteger CompDelta { get; set; }

        [Parameter("uint256", "platformTokenSupplyIndex", 4, false)]
        public BigInteger CompSupplyIndex { get; set; }
    }
}