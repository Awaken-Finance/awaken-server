using System;
using Volo.Abp.EventBus;

namespace AwakenServer.Chains;

[EventName("NewChainEvent")]
public class NewChainEvent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int BlocksPerDay { get; set; }
    public long LatestBlockHeight { get; set; }
        
    public int AElfChainId { get; set; }
}