using System.Collections.Generic;

namespace AwakenServer.Activity;

public class ActivityOptions
{
    public List<Activity> ActivityList { get; set; }
}

public class Activity
{
    public int ActivityId { get; set; }
    public string Type { get; set; }
    public long BeginTime { get; set; }  
    public long EndTime { get; set; }   
    public List<string> TradePairs { get; set; }
    public List<string> WhiteList { get; set; }
}