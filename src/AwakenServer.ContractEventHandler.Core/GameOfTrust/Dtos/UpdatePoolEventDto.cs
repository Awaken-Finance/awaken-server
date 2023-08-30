using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Dtos
{
    [Event("UpdatePool")]
    public class UpdatePoolEventDto : IEventDTO
    {
        [Parameter("uint256", "pid", 1, false)]
        public int Pid { get; set; }

        [Parameter("uint256", "marketCap", 2, false)]
        public BigInteger MarketCap { get; set; }

        [Parameter("uint256", "rewardRate", 3, false)]
        public int RewardRate { get; set; }

        [Parameter("uint256", "unlockCycle", 4, false)]
        public long UnlockCycle { get; set; }

        [Parameter("uint256", "totalAmountLimit", 5, false)]
        public BigInteger TotalAmountLimit { get; set; }
    }
}