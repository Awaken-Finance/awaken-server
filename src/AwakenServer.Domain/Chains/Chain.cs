using System;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Entities;
using JetBrains.Annotations;
using Nest;

namespace AwakenServer.Chains
{
    public class Chain : AwakenEntity<string>,IIndexBuild
    {
        [Keyword]
        public override string Id { get; set; }
        [NotNull] 
        [Keyword]
        public virtual string Name { get; set; }
        [Keyword]
        public int AElfChainId { get; set; }
    }
}