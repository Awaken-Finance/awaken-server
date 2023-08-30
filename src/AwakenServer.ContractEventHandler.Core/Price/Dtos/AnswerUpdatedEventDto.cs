using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Price.Dtos
{
    [Event("AnswerUpdated")]
    public class AnswerUpdatedEventDto : IEventDTO
    {
        [Parameter("int256", "current", 1, true)]
        public BigInteger Current { get; set; }

        [Parameter("uint256", "roundId", 2, true)] 
        public BigInteger RoundId { get; set; }
        
        [Parameter("uint256", "updatedAt", 3, false)] 
        public long UpdatedAt { get; set; }
    }
}