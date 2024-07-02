namespace AwakenServer.Trade.Dtos;

public class SyncRecordGrainDto : SyncRecordDto
{
    public double Token0PriceInUsd { get; set; }
    public double Token1PriceInUsd { get; set; }
}