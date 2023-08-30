using System;
using Nest;

namespace AwakenServer.Entities
{
    public class MultiChainEntity<TKey> : AwakenEntity<TKey>, IMultiChain
    {
        [Keyword]
        public virtual string ChainId { get; set; }


        protected MultiChainEntity()
        {
        }

        protected MultiChainEntity(TKey id)
            : base(id)
        {
        }
    }
}