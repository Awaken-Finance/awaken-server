using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs.MassiveFarm
{
    [Event("HalvingPeriodSet")]
    public class HalvingPeriodSet: IEventDTO
    {
        [Parameter("uint256", "period1", 1, false)]
        public long Period1 { get; set; }
        [Parameter("uint256", "period2", 2, false)]
        public long Period2 { get; set; }
    }
}