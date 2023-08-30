using System;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Dividend.Entities
{
    public class DividendBase : MultiChainEntity<Guid>
    {
        [Keyword] public override Guid Id { get; set; }
        [Keyword] public string Address { get; set; }
    }

    public class Dividend : DividendBase, IIndexBuild
    {
        public int TotalWeight { get; set; }
    }
}