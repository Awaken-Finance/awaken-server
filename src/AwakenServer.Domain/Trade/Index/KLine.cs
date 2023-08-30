using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index
{
    public class KLine : KLineBase, IIndexBuild
    {
        public KLine()
        {
        }

        public KLine(Guid id)
            : base(id)
        {
        }
    }
}