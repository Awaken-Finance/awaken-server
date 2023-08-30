using AwakenServer.Farms.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.Farms
{
    [EventName("Farm.FarmRecordChangedEto")]
    public class FarmRecordChangedEto: FarmRecord
    {
    }
}