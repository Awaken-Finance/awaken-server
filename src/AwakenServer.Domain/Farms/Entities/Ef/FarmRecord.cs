using System;

namespace AwakenServer.Farms.Entities.Ef
{
    public class FarmRecord: FarmRecordBase
    {
        public Guid PoolId { get; set; }
        public Guid FarmId { get; set; }
        public Guid TokenId { get; set; }
    }
}