using Orleans;

namespace AwakenServer.Trade.Dtos;

[GenerateSerializer]
public class SyncRecordGrainDto : SyncRecordDto
{
    [Id(0)]
    public double Token0PriceInUsd { get; set; }
    [Id(1)]
    public double Token1PriceInUsd { get; set; }
}