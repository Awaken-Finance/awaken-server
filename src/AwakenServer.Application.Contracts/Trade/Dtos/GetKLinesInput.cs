using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Trade.Dtos
{
    public class GetKLinesInput : IValidatableObject
    {
        [Required]
        public string ChainId { get; set; }
        [Required]
        public Guid TradePairId { get; set; }
        [Required]
        public int Period { get; set; }
        [Required]
        public long TimestampMin { get; set; }
        [Required]
        public long TimestampMax { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if ((TimestampMax - TimestampMin) / Period > 1000 * 1000)
            {
                yield return new ValidationResult(
                    "Out of the valid time range!",
                    new[] {"TimestampMin", "TimestampMax"}
                );
            }
        }
    }
}