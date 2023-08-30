using System;

namespace AwakenServer.Farms.Entities.Ef
{
    public class FarmPool : EditableStateFarmPool
    {
        public Guid SwapTokenId { get; set; }
        public Guid Token1Id { get; set; }
        public Guid Token2Id { get; set; }
    }
}