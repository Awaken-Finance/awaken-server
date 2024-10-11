using System;
using AwakenServer.Entities;

namespace AwakenServer.Activity;

public class ActivityBase : MultiChainEntity<Guid>
{
    public int ActivityId { get; set; }
}