using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken
{
    [Event("Mint")]
    public class Mint : IEventDTO
    {
        [Parameter("address", "minter", 1, false)]
        public string Minter { get; set; }

        [Parameter("uint256", "mintAmount", 2, false)]
        public BigInteger MintAmount { get; set; }

        [Parameter("uint256", "mintTokens", 3, false)]
        public BigInteger MintTokens { get; set; }

        [Parameter("string", "channel", 4, false)]
        public string Channel { get; set; }
    }
}