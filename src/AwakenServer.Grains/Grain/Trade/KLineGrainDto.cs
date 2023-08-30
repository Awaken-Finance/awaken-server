using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Trade;

public class KLineGrainDto : KLineBase
{
    public string GrainId { get; set; }
}