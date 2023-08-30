using AwakenServer.Entities.GameOfTrust.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.GameOfTrust
{   
    [EventName("GameOfTrust.GameOfTrustRecordCreated")]
    public class GameOfTrustRecordCreatedEto:GameOfTrustRecord
    {
    }
}