using System;
using AwakenServer.Farms.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.Farms
{
    [EventName("Farm.FarmChanged")]
    public class FarmChangedEto: Farm
    {
        public FarmChangedEto(Guid id)
        {
            Id = id;
        }
    }
}