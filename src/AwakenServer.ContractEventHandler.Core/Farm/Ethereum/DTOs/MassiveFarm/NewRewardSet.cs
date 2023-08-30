using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.MassiveFarm
{
    [Event("NewRewardSet")]
    public class NewRewardSet: IEventDTO
    {
        [Parameter("uint256", "startBlock", 1, false)]
        public long StartBlock { get; set; }
        [Parameter("uint256", "endBlock", 2, false)]
        public long EndBlock { get; set; }
        [Parameter("uint256", "usdtPerBlock", 3, false)]
        public BigInteger UsdtPerBlock { get; set; }
    }
}