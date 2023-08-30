using AwakenServer.Farms.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.Farms
{

    [EventName("Farm.FarmUserInfoChangedEto")]
    public class FarmUserInfoChangedEto : FarmUserInfo
    {
        public FarmRecord Record { get; set; }
    }
}