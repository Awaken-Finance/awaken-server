using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("MarketExited")]
    public class MarketExited : IEventDTO
    {
        [Parameter("address", "gToken", 1, false)]
        public string CToken { get; set; }

        [Parameter("address", "account", 2, false)]
        public string Account { get; set; }
    }
}