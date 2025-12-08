using System.Collections.Generic;

namespace AwakenServer.Activity;

public class ActivityOptions
{
    public List<Activity> ActivityList { get; set; } = new();
    public List<string> PricingTokens { get; set; } = new();
}

public class Activity
{
    public int ActivityId { get; set; }
    public string Type { get; set; }
    public long BeginTime { get; set; }  
    public long EndTime { get; set; }
    public List<string> TradePairs { get; set; } = new();
    public List<string> WhiteList { get; set; } = new();
}