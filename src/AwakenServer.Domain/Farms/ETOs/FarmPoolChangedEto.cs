using AwakenServer.Farms.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.Farms
{
    [EventName("Farm.FarmPoolChanged")]
    public class FarmPoolChangedEto : FarmPool
    {
    }
}