using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller
{
    [Event("ActionPaused")]
    public class ActionPaused : IEventDTO
    {
        [Parameter("address", "gToken", 1, false)]
        public string CToken { get; set; }

        [Parameter("string", "action", 2, false)]
        public string Action { get; set; }

        [Parameter("bool", "pauseState", 3, false)]
        public bool PauseState { get; set; }
    }
}