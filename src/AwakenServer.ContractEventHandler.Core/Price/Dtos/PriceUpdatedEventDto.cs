using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Price.Dtos
{
    [Event("PriceUpdated")]
    public class PriceUpdatedEventDto : IEventDTO
    {
        [Parameter("address", "underlying", 1, false)]
        public string Underlying { get; set; }

        [Parameter("string", "symbol", 2, false)] 
        public string Symbol { get; set; }
        
        [Parameter("uint256", "price", 3, false)] 
        public BigInteger Price { get; set; }
        
        [Parameter("uint256", "timestamp", 4, false)] 
        public long Timestamp { get; set; }
    }
}