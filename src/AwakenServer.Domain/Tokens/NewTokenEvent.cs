using System;
using Volo.Abp.EventBus;

namespace AwakenServer.Tokens;

[EventName("NewTokenEvent")]
public class NewTokenEvent
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}