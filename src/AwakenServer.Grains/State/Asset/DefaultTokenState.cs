namespace AwakenServer.Grains.State.Asset;

[GenerateSerializer]
public class DefaultTokenState
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string Address { get; set; }
    [Id(2)]
    public string Symbol { get; set; }
}