using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Trade.Ethereum.Dtos
{
    [Event("Mint")]
    public class MintEventDto : IEventDTO
    {
        [Parameter("address", "sender", 1, true)]
        public string Sender { get; set; }

        [Parameter("uint256", "amount0", 2, false)] 
        public BigInteger Amount0 { get; set; }
        
        [Parameter("uint256", "amount1", 3, false)] 
        public BigInteger Amount1 { get; set; }
        
        [Parameter("address", "to", 4, true)]
        public string To { get; set; }
        
        [Parameter("uint256", "liquidity", 5, false)] 
        public BigInteger Liquidity { get; set; }

        [Parameter("string", "channel", 6, false)] 
        public string Channel { get; set; }
    }
}