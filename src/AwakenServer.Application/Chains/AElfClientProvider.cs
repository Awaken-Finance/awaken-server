using System;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Client.Service;
using AElf.Types;
using AwakenServer.Commons;
using AwakenServer.Monitor;
using AwakenServer.Tokens;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AwakenServer.Chains
{
    public interface IAElfClientProvider : IBlockchainClientProvider
    {
        Task<long> GetTransactionFeeAsync(string chainName, string transactionId);
        Task<int> ExistTransactionAsync(string chainName, string transactionHash);
        Task<TokenInfo> GetTokenInfoFromChainAsync(string chainName, string address, string symbol);
    }

    public class AElfClientProvider : IAElfClientProvider
    {
        private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;
        private readonly IIndicatorScope _indicatorScope;
        private readonly ILogger<AElfClientProvider> _logger;

        public AElfClientProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
            IIndicatorScope indicatorScope,
            ILogger<AElfClientProvider> logger)
        {
            _blockchainClientFactory = blockchainClientFactory;
            _indicatorScope = indicatorScope;
            _logger = logger;
        }

        public string ChainType { get; } = "AElf";

        public async Task<long> GetBlockNumberAsync(string chainName)
        {
            var client = _blockchainClientFactory.GetClient(chainName);
            
            var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
                MonitorAelfClientType.GetBestChainHeightAsync.ToString());
            var chain = await client.GetChainStatusAsync();
            _indicatorScope.End(interIndicator);
            
            return chain.BestChainHeight;
        }

        public async Task<TokenDto> GetTokenInfoAsync(string chainName, string address, string symbol)
        {
            var client = _blockchainClientFactory.GetClient(chainName);
            if (address.IsNullOrWhiteSpace())
            {
                address = (await client.GetContractAddressByNameAsync(
                    HashHelper.ComputeFrom("AElf.ContractNames.Token"))).ToBase58();
            }

            var token = await GetTokenInfoFromChainAsync(chainName, address, symbol);
            return new TokenDto
            {
                Address = address,
                Decimals = token.Decimals,
                Symbol = token.Symbol
            };
        }

        public async Task<TokenInfo> GetTokenInfoFromChainAsync(string chainName, string address, string symbol)
        {
            var client = _blockchainClientFactory.GetClient(chainName);
            var paramGetBalance = new GetTokenInfoInput
            {
                Symbol = symbol
            };
            
            var generateIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
                MonitorAelfClientType.GenerateTransactionAsync.ToString());
            var transactionGetToken =
                await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(ChainsInitOptions.PrivateKey), address,
                    "GetTokenInfo",
                    paramGetBalance);
            _indicatorScope.End(generateIndicator);
            
            var txWithSignGetToken = client.SignTransaction(ChainsInitOptions.PrivateKey, transactionGetToken);
            
            var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
                MonitorAelfClientType.ExecuteTransactionAsync.ToString());
            var transactionGetTokenResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSignGetToken.ToByteArray().ToHex()
            });
            _indicatorScope.End(interIndicator);
            
            return
                TokenInfo.Parser.ParseFrom(
                    ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));
        }

        public async Task<long> GetTransactionFeeAsync(string chainName, string transactionId)
        {
            try
            {
                var client = _blockchainClientFactory.GetClient(chainName);
                
                var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
                    MonitorAelfClientType.GetTransactionResultAsync.ToString());
                var result = await client.GetTransactionResultAsync(transactionId);
                _indicatorScope.End(interIndicator);
                
                if (result == null)
                {
                    return 0;
                }
                var transactionFeeCharged = TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(result.Logs.First(l => l.Name == nameof(TransactionFeeCharged)).NonIndexed));
                return transactionFeeCharged.Amount;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetTransactionFeeAsync fail.");
                return 0;
            }
        }

        public async Task<int> ExistTransactionAsync(string chainName, string transactionHash)
        {
            try
            {
                var client = _blockchainClientFactory.GetClient(chainName);
                var result = await client.GetTransactionResultAsync(transactionHash);
                if (result == null)
                {
                    return -1;
                }

                return !string.IsNullOrWhiteSpace(result.Status)
                       && (result.Status.Equals(TransactionResultStatus.Pending.ToString(),
                               StringComparison.OrdinalIgnoreCase)
                           || result.Status.Equals(TransactionResultStatus.Mined.ToString(),
                               StringComparison.OrdinalIgnoreCase) 
                           || result.Status.Equals(TransactionResultStatus.PendingValidation.ToString(),
                               StringComparison.OrdinalIgnoreCase))
                    ? 1
                    : 0;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get the current status of a transaction {chainName}:{transactionHash} fail.", chainName, transactionHash);
                return -1;
            }
        }
    }
}