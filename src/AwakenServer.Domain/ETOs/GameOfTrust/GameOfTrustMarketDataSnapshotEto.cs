using System;
using AwakenServer.Entities.GameOfTrust.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.GameOfTrust
{   
    [EventName("GameOfTrust.MarketDataChanged")]
    public class GameOfTrustMarketDataSnapshotEto: GameOfTrustMarketData
    {
        public GameOfTrustMarketDataSnapshotEto(Guid id)
        {
            Id = id;
        }
    }
}