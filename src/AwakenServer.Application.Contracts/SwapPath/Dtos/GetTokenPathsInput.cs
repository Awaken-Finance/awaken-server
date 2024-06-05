using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.SwapTokenPath.Dtos;

public class GetTokenPathsInput : PagedAndSortedResultRequestDto, IValidatableObject
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string StartSymbol { get; set; }
    [Required]
    public string EndSymbol { get; set; }
    public int MaxDepth { get; set; } = 3;
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxDepth > 10)
        {
            yield return new ValidationResult(
                "Out of the valid max search depth range!",
                new[] {"MaxDepth"}
            );
        }
    }
}