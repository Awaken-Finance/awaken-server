using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Dtos
{
    [Event("UpToStandard")]
    public class UpToStandardEventDto : IEventDTO
    {
        [Parameter("uint256", "pid", 1, false)]
        public int Pid { get; set; }

        [Parameter("uint256", "blocks", 2, false)]
        public long Blocks { get; set; }

        [Parameter("uint256", "currentMarketcap", 3, false)]
        public BigInteger CurrentMarketcap { get; set; }
    }
}