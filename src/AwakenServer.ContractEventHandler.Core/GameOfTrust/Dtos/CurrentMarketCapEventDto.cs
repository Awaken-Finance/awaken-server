using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Dtos
{
    [Event("CurrentMarketCap")]
    public class CurrentMarketCapEventDto : IEventDTO
    {
        [Parameter("uint256", "totalSupply", 1, false)]
        public BigInteger TotalSupply { get; set; }

        [Parameter("uint256", "marketCap", 2, false)]
        public BigInteger MarketCap { get; set; }

        [Parameter("uint256", "averagePrice", 3, false)]
        public BigInteger AveragePrice { get; set; }
    }
}