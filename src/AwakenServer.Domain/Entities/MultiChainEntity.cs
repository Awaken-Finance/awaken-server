using System;
using Nest;
using Orleans;

namespace AwakenServer.Entities
{
    [GenerateSerializer]
    public class MultiChainEntity<TKey> : AwakenEntity<TKey>, IMultiChain
    {
        [Keyword]
        [Id(0)] public virtual string ChainId { get; set; }


        protected MultiChainEntity()
        {
        }

        protected MultiChainEntity(TKey id)
            : base(id)
        {
        }
    }
}