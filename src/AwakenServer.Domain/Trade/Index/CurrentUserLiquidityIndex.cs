using System;
using AElf.Indexing.Elasticsearch;

namespace AwakenServer.Trade.Index;

public class CurrentUserLiquidityIndex : CurrentUserLiquidity, IIndexBuild
{
    public CurrentUserLiquidityIndex()
    {
    }
}