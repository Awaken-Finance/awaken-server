namespace AwakenServer.Grains.Grain.Chain;

public class ChainGrainDto : Chains.Chain
{
    public long LatestBlockHeight { get; set; }

    public long? LatestBlockHeightExpireMs { get; set; }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Name);
    }
}