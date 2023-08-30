using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("MarketEntered")]
    public class MarketEntered : IEventDTO
    {
        [Parameter("address", "gToken", 1, false)]
        public string CToken { get; set; }

        [Parameter("address", "account", 2, false)]
        public string Account { get; set; }
    }
}