using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("MarketListed")]
    public class MarketListed : IEventDTO
    {
        [Parameter("address", "gToken", 1, false)]
        public string CToken { get; set; }
    }
}