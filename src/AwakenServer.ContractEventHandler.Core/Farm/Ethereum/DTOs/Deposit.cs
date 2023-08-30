using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs
{
    [Event("Deposit")]
    public class Deposit: IEventDTO
    {
        [Parameter("address", "user", 1, true)]
        public string User { get; set; }

        [Parameter("uint256", "pid", 2, true)]
        public int Pid { get; set; }

        [Parameter("uint256", "amount", 3, false)]
        public BigInteger Amount { get; set; }
    }
}