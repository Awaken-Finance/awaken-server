using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs
{
    [Event("PoolAdded")]
    public class PoolAdded: IEventDTO
    {
        [Parameter("address", "token", 1, false)]
        public string SwapToken { get; set; }

        [Parameter("uint256", "pid", 2, false)]
        public int Pid { get; set; }

        [Parameter("uint256", "allocationPoint", 3, false)]
        public int AllocationPoint { get; set; }
        
        [Parameter("uint256", "lastRewardBlockHeight", 4, false)]
        public long LastRewardBlockHeight { get; set; }

        [Parameter("uint256", "poolType", 5, false)] 
        public int PoolType { get; set; }
    }
}