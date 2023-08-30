// using System.Numerics;
// using Nethereum.ABI.FunctionEncoding.Attributes;
//
// namespace AwakenServer.ContractEventHandler.Debit.DTOs.Comptroller
// {
//     [Event("NewLiquidationIncentive")]
//     public class NewLiquidationIncentive: IEventDTO
//     {
//         [Parameter("uint256", "oldLiquidationIncentiveMantissa", 1, false)]
//         public BigInteger OldLiquidationIncentiveMantissa { get; set; }
//         [Parameter("uint256", "newLiquidationIncentiveMantissa", 2, false)]
//         public BigInteger NewLiquidationIncentiveMantissa { get; set; }
//     }
// }