using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    public class FarmPoolBaseDto: EntityDto<Guid>
    {
        public int Pid { get; set; }
        public PoolType PoolType { get; set; }
    }
}