namespace AwakenServer.Grains.Grain;

public class GrainResultDto<T> : GrainResultDto
{
    public T Data { get; set; }
}

public class GrainResultDto
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
}