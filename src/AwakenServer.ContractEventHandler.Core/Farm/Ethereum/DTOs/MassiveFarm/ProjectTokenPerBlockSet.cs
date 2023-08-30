using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.MassiveFarm
{
    [Event("DistributeTokenPerBlockSet")]
    public class ProjectTokenPerBlockSet: IEventDTO
    {
        [Parameter("uint256", "newDistributeTokenPerBlock1", 1, false)]
        public BigInteger NewProjectTokenPerBlock1 { get; set; }
        [Parameter("uint256", "newDistributeTokenPerBlock2", 2, false)]
        public BigInteger NewProjectTokenPerBlock2 { get; set; }
    }
}