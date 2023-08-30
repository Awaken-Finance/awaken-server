using System;
using Nest;

namespace AwakenServer.Entities.GameOfTrust
{
    public abstract class GameOfTrustBase : MultiChainEntity<Guid>
    {   
        public int Pid { get; set; }
        [Keyword]
        public string UnlockMarketCap { get; set; }
        public string RewardRate { get; set; }
        public long UnlockCycle { get; set; }
        public long UnlockHeight { get; set; }
        public string TotalAmountLimit { get; set; }
        public long StartHeight { get; set; }
        public long EndHeight { get; set; }
        public long BlocksDaily { get; set; }
        // contract address
        public string Address { get; set; }
    }


    public class GameOfTrustWithToken : GameOfTrustBase
    {
        public Tokens.Token DepositToken { get; set; }
        public Tokens.Token HarvestToken { get; set; }
    }
}