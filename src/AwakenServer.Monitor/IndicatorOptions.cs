namespace AwakenServer.Monitor;

public class IndicatorOptions
{
    public bool IsEnabled { get; set; }
    public string Application { get; set; } = "Awaken";
    public string Module { get; set; } = "Api";
}