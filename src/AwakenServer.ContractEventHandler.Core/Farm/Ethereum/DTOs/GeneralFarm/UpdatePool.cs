using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.GeneralFarm
{
    [Event("UpdatePool")]
    public class UpdatePool: IEventDTO
    { 
        [Parameter("uint256", "pid", 1, false)]
        public int Pid { get; set; }
        [Parameter("uint256", "distributeTokenAmount", 2, false)]
        public BigInteger ProjectTokenAmount { get; set; }
        [Parameter("uint256", "updateBlockHeight", 3, false)]
        public long UpdateBlockHeight { get; set; }
    }
}