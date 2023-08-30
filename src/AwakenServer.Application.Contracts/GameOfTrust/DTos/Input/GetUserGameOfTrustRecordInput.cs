
namespace AwakenServer.GameOfTrust.DTos.Input
{
    public class GetUserGameOfTrustRecordInput:GetUserGameOfTrustsInput
    {
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
        public long UnlockMarketCap { get; set; }
        public BehaviorType? type { get; set; }
    }
}