using System;

namespace AwakenServer.Entities.GameOfTrust.Ef
{
    public class GameOfTrust : GameOfTrustBase
    {
        public Guid DepositTokenId { get; set; }
        public Guid HarvestTokenId { get; set; }
        public string TotalValueLocked { get; set; }
        public string FineAmount { get; set; }
    }
}