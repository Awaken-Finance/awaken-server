using System;
using AwakenServer.Entities;
using Orleans;

namespace AwakenServer.Activity;

public class ActivityBase : MultiChainEntity<Guid>
{
    public int ActivityId { get; set; }
}