using System;
using AwakenServer.GameOfTrust.DTos.Dto;

namespace AwakenServer.GameOfTrust.DTos
{
    public class GameOfTrustDto: MarketCapsDto
    {
        public string RewardRate { get; set; }
        public long UnlockCycle { get; set; }
        public long UnlockHeight { get; set; }
        public string TotalAmountLimit { get; set; }
        public long StartHeight { get; set; }
        public long EndHeight { get; set; }
        public long BlocksDaily { get; set; }
        public TokenDto DepositToken { get; set; }
        public TokenDto HarvestToken { get; set; }
        public string Address { get; set; }
        public string ChainId { get; set; }
        public string TotalValueLocked { get; set; }
        public string FineAmount { get; set; }
    }
}