namespace AwakenServer.Grains.State.Chain;

public class ChainState
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int AElfChainId { get; set; }
    public long LatestBlockHeight { get; set; }
    public long LatestBlockHeightExpireMs { get; set; }
}