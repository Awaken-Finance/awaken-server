using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Dtos
{
    [Event("HarvestLiquidateReward")]
    public class HarvestLiquidateRewardEventDto : IEventDTO
    {
        [Parameter("uint256", "pid", 1, false)]
        public int Pid { get; set; }

        [Parameter("address", "receiver", 2, false)]
        public string Receiver { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger Amount { get; set; }
        
    }
}