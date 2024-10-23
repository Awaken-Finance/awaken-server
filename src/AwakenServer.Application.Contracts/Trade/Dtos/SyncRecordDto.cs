using System;
using System.Collections.Generic;
using Orleans;

namespace AwakenServer.Trade.Dtos;

public class SyncRecordPageResultDto
{
    public long TotalCount { get; set; }
    public List<SyncRecordDto> Data { get; set; }
}

[GenerateSerializer]
public class SyncRecordDto
{
    [Id(0)]
    public string ChainId { get; set; }
    [Id(1)]
    public string PairAddress { get; set; }
    [Id(2)]
    public Guid PairId { get; set; }
    [Id(3)]
    public string SymbolA { get; set; }
    [Id(4)]
    public string SymbolB { get; set; }
    [Id(5)]
    public long ReserveA { get; set; }
    [Id(6)]
    public long ReserveB { get; set; }
    [Id(7)]
    public long Timestamp { get; set; }
    [Id(8)]
    public long BlockHeight { get; set; }
    [Id(9)]
    public string TransactionHash { get; set; }
    [Id(10)]
    public bool IsRevert { get; set; }
}

public class SyncRecordResultDto
{
    public List<SyncRecordDto> GetSyncRecords { get; set; }
}