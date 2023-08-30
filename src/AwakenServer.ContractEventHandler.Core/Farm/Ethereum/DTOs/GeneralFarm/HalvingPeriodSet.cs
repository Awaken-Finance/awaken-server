using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.GeneralFarm
{
    [Event("HalvingPeriodSet")]
    public class HalvingPeriodSet: IEventDTO
    {
        [Parameter("uint256", "period", 1, false)]
        public long Period { get; set; }
    }
}