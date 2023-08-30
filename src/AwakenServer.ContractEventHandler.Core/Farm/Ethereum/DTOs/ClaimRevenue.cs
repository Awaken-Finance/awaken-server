using System.Numerics;
using AwakenServer.Farms;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs
{
    [Event("ClaimRevenue")]
    public class ClaimRevenue: IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }

        [Parameter("uint256", "pid", 2, true)]
        public int Pid { get; set; }
        
        [Parameter("address", "tokenAddress", 3, false)]
        public string TokenAddress { get; set; }
        [Parameter("uint256", "tokenType", 4, false)]
        public DividendTokenType DividendTokenType { get; set; }

        [Parameter("uint256", "amount", 5, false)]
        public BigInteger Amount { get; set; }
    }
}