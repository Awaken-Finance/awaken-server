using System;

namespace AwakenServer.Price
{
    public class OtherLpToken : OtherLpTokenBase
    {
        public Guid Token0Id { get; set; }
        
        public Guid Token1Id { get; set; }

        public OtherLpToken()
        {
        }
        
        public OtherLpToken(Guid id) : base(id)
        {
        }
    }
}