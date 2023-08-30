using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Dtos
{   
    [Event("AddPool")]
    public class AddPoolEventDto:IEventDTO
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

        [Parameter("uint256", "startBlock", 6, false)]
        public long StartBlock { get; set; }

        [Parameter("uint256", "stakeEndBlock", 7, false)]
        public long StakeEndBlock { get; set; }

        [Parameter("uint256", "blocksDaily", 8, false)]
        public long BlocksDaily { get; set; }

        [Parameter("address", "depostToken", 9, false)]
        public string DepositToken { get; set; }

        [Parameter("address", "harvestToken", 10, false)]
        public string HarvestToken { get; set; }
        
    }
}