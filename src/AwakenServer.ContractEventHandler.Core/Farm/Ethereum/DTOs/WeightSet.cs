using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs
{
    [Event("WeightSet")]
    public class WeightSet : IEventDTO
    {
        [Parameter("uint256", "pid", 1, false)]
        public int Pid { get; set; }
        [Parameter("uint256", "newAllocationPoint", 2, false)]
        public int NewAllocationPoint { get; set; }
    }
}