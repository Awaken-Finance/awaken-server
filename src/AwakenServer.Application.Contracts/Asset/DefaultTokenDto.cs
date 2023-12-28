using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Nethereum.Util;
using AElf.Types;

namespace AwakenServer.Asset;

public class DefaultTokenDto : IValidatableObject
{
    [Required] public string Address { get; set; }
    [Required] public string TokenSymbol { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        Exception exception = null;

        try
        {
            AElf.Types.Address.FromBase58(Address);
        }
        catch (Exception e)
        {
            exception = e;
        }

        if (exception != null)
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
        Exception exception = null;

        try
        {
            AElf.Types.Address.FromBase58(Address);
        }
        catch (Exception e)
        {
            exception = e;
        }

        if (exception != null)
        {
            yield return new ValidationResult("Address is invalid", new[] { nameof(Address) });
        }
    }
}