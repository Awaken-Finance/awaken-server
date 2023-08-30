using System;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AwakenServer.Debits;
using AwakenServer.Debits.Entities.Ef;

namespace AwakenServer.ContractEventHandler.Debit.Helpers
{
    public static class RecordGeneratorHelper
    {
        public static CTokenRecord GenerateCTokenRecord(EventContext eventContext,
            CToken cTokenInfo, string user, BehaviorType behaviorType, string underlyingTokenAmount = null,
            string cTokenAmount = null, string channel = null)
        {
            var txHash = eventContext.TransactionId;
            var datetime = eventContext.BlockTime;
            return GenerateCTokenRecord(txHash, datetime, cTokenInfo, user,
                behaviorType, underlyingTokenAmount,
                cTokenAmount, channel);
        }

        public static CTokenRecord GenerateCTokenRecord(ContractEventDetailsDto contractEventDetailsDto,
            CToken cTokenInfo, string user, BehaviorType behaviorType, string underlyingTokenAmount = null,
            string cTokenAmount = null, string channel = null)
        {
            var txHash = contractEventDetailsDto.TransactionHash;
            var datetime = DateTimeHelper.FromUnixTimeMilliseconds(contractEventDetailsDto.Timestamp * 1000);
            return GenerateCTokenRecord(txHash, datetime, cTokenInfo, user,
                behaviorType, underlyingTokenAmount,
                cTokenAmount, channel);
        }

        private static CTokenRecord GenerateCTokenRecord(string txHash, DateTime datetime,
            CToken cTokenInfo, string user, BehaviorType behaviorType, string underlyingTokenAmount = null,
            string cTokenAmount = null, string channel = null)
        {
            return new()
            {
                TransactionHash = txHash,
                User = user,
                UnderlyingTokenAmount = underlyingTokenAmount ?? "0",
                Date = datetime,
                BehaviorType = behaviorType,
                CTokenId = cTokenInfo.Id,
                Channel = channel ?? string.Empty,
                CompControllerId = cTokenInfo.CompControllerId,
                UnderlyingAssetTokenId = cTokenInfo.UnderlyingTokenId,
                CTokenAmount = cTokenAmount ?? "0"
            };
        }
    }
}