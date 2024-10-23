namespace AwakenServer.Grains.Grain.Chain;

[GenerateSerializer]
public class ChainGrainDto
{
   [Id(0)] public string Id { get; set; }
   [Id(1)] public string Name { get; set; }
   [Id(2)] public int AElfChainId { get; set; }
   [Id(3)] public long LatestBlockHeight { get; set; }
   [Id(4)] public long? LatestBlockHeightExpireMs { get; set; }

    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(Name);
    }
}