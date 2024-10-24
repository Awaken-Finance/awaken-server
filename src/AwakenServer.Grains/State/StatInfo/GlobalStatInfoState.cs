namespace AwakenServer.Grains.State.StatInfo;

[GenerateSerializer]
public class GlobalStatInfoState
{
    [Id(0)]
    public double Tvl { get; set; }
}