using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Asset;

public class DefaultTokenDto
{
    [Required] public string Address { get; set; }
    [Required] public string TokenSymbol { get; set; }
    
}

public class SetDefaultTokenDto : DefaultTokenDto
{
}

public class GetDefaultTokenDto 
{
    [Required] public string Address { get; set; }
}