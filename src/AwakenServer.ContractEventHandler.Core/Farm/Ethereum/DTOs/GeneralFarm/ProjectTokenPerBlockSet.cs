using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.GeneralFarm
{
    [Event("DistributeTokenPerBlockSet")]
    public class ProjectTokenPerBlockSet: IEventDTO
    {
        [Parameter("uint256", "newDistributeTokenPerBlock", 1, false)]
        public BigInteger NewProjectTokenPerBlock { get; set; }
    }
}