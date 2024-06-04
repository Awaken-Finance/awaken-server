using System.Collections.Generic;

namespace AwakenServer.SwapTokenPath.Dtos;


public class TokenPathDto
{
    public double FeeRate { get; set; }
    public List<PathNodeDto> Path { get; set; } = new List<PathNodeDto>();
}

public class PathNodeDto
{
    public string Token0Symbol { get; set; }
    public string Token1Symbol { get; set; }
    public string Address { get; set; }
    public double FeeRate { get; set; }
}