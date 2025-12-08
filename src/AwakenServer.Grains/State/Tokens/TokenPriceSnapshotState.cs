namespace AwakenServer.Grains.State.Tokens;

[GenerateSerializer]
public class TokenPriceSnapshotState : TokenPriceBase
{
    [Id(0)]
    public string TimeStamp { get; set; }
}