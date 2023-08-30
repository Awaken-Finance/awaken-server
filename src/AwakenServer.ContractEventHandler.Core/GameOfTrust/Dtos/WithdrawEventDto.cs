using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Dtos
{
    [Event("Withdraw")]
    public class WithdrawEventDto : IEventDTO
    {
        [Parameter("uint256", "pid", 1, false)]
        public int Pid { get; set; }

        [Parameter("address", "receiver", 2, false)]
        public string Receiver { get; set; }

        [Parameter("uint256", "amountProjectToken", 3, false)]
        public BigInteger AmountProjectToken { get; set; }

        [Parameter("uint256", "amountToken", 4, false)]
        public BigInteger AmountToken { get; set; }

        [Parameter("uint256", "fine", 5, false)]
        public BigInteger Fine { get; set; }
    }
}