using System;

namespace AwakenServer.Farms.Entities.Ef
{
    public class FarmUserInfo : FarmUserInfoBase
    {
        public Guid PoolId { get; set; }
    }
}