using System;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade;

public class TradePairInfoIndex:AwakenEntity<Guid>,IIndexBuild
{
    [Keyword]
    public override Guid Id { get; set; }
    [Keyword]
    public string ChainId { get; set; }

    [Keyword]
    public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    [Keyword]
    public string PreviousBlockHash { get; set; }

    public bool IsDeleted { get; set; }
    [Keyword] 
    public string Address { get; set; }
    [Keyword] 
    public string Token0Symbol { get; set; }
    [Keyword] 
    public string Token1Symbol { get; set; }
    [Keyword]
    public Guid Token0Id { get; set; }
    [Keyword]
    public Guid Token1Id { get; set; }
    [Keyword]
    public double FeeRate { get; set; }
    [Keyword]
    public bool IsTokenReversed { get; set; }
}