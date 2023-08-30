using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Price
{
    public class OtherLpTokenBase :  MultiChainEntity<Guid>
    {
        [Keyword]
        public string Address { get; set; }
        
        [Keyword]
        public string Reserve0 { get; set; }
        
        public double Reserve0Value { get; set; }
        
        [Keyword]
        public string Reserve1 { get; set; }
        
        public double Reserve1Value { get; set; }
        
        protected OtherLpTokenBase()
        {
        }
        
        protected OtherLpTokenBase(Guid id) : base(id)
        {
        }
    }
}