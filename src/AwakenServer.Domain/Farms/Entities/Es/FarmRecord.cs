using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace AwakenServer.Farms.Entities.Es
{
    public class FarmRecord: FarmRecordBase, IIndexBuild
    {
        [Keyword] public override Guid Id { get; set; }
        public double DecimalAmount { get; set; }
        public FarmBase FarmInfo { get; set; }
        public FarmPoolBase PoolInfo { get; set; }
        public Tokens.Token TokenInfo { get; set; }
    }
}