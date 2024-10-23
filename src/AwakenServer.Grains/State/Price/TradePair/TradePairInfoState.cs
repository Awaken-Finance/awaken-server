using System;

namespace AwakenServer.Grains.State.Price;

[GenerateSerializer]
public class TradePairInfoState
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public string ChainId { get; set; }

    [Id(2)]
    public string BlockHash { get; set; }

    [Id(3)]
    public long BlockHeight { get; set; }


    [Id(4)]
    public string PreviousBlockHash { get; set; }

    [Id(5)]
    public bool IsDeleted { get; set; }

    [Id(6)]
    public string Address { get; set; }

    [Id(7)]
    public string Token0Symbol { get; set; }

    [Id(8)]
    public string Token1Symbol { get; set; }

    [Id(9)]
    public Guid Token0Id { get; set; }

    [Id(10)]
    public Guid Token1Id { get; set; }

    [Id(11)]
    public double FeeRate { get; set; }

    [Id(12)]
    public bool IsTokenReversed { get; set; }
}