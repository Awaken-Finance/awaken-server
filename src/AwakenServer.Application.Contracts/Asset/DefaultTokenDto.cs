using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElf;

namespace AwakenServer.Asset;

public class DefaultTokenDto : IValidatableObject
{
    [Required] public string Address { get; set; }
    [Required] public string TokenSymbol { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AddressHelper.VerifyFormattedAddress(Address))
        {
           yield return new ValidationResult("Address is invalid", new[] { nameof(Address) }); 
        }
    }
}

public class SetDefaultTokenDto : DefaultTokenDto
{
}

public class GetDefaultTokenDto : IValidatableObject
{
    [Required] public string Address { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AddressHelper.VerifyFormattedAddress(Address))
        {
            yield return new ValidationResult("Address is invalid", new[] { nameof(Address) }); 
        }
    }
}