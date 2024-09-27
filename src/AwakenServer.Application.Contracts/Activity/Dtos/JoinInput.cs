using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AElf;

namespace AwakenServer.Activity.Dtos;

public class JoinInput : ActivityBaseDto, IValidatableObject
{
    [Required] public string Message { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public string PublicKey { get; set; }
    [Required] public string Address { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AddressHelper.VerifyFormattedAddress(Address))
        {
            yield return new ValidationResult("Invalid address", new []{Address});
        }
    }
}