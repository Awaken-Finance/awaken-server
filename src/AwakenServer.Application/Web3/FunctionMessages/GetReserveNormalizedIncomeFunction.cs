using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace AwakenServer.Web3.FunctionMessages
{
    [Function("getReserveNormalizedIncome", "uint256")]
    public class GetReserveNormalizedIncomeFunction: FunctionMessage
    {
        [Parameter("address", "asset", 1)]
        public virtual string Asset { get; set; }
    }
}