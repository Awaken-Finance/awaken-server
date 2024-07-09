using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Route.Dtos;

public enum RouteType
{
    ExactIn,
    ExactOut
}
public class GetBestRoutesInput : IValidatableObject
{
    [Required] public string ChainId { get; set; }
    [Required] public string SymbolIn { get; set; }
    [Required] public string SymbolOut { get; set; }
    [Required] public RouteType RouteType { get; set; }
    public long AmountIn { get; set; }
    public long AmountOut { get; set; }

    public int ResultCount { get; set; } = 1;
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AmountIn <= 0 && AmountOut <= 0)
        {
            yield return new ValidationResult(
                "Either AmountIn or AmountOut must be set to a value greater than 0!",
                new[] {"AmountIn or AmountOut"}
            );
        }
    }
}