using System;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.MultiToken;
using AElf.Client.Service;
using AElf.Types;
using AwakenServer.Tokens;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.IO;
using Serilog;
using TransactionFeeCharged = AElf.Contracts.MultiToken.TransactionFeeCharged;
using TokenInfo = AElf.Contracts.MultiToken.TokenInfo;
using JsonConvert = Newtonsoft.Json.JsonConvert;

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
        private readonly ILogger<AElfClientProvider> _logger;
        private const string FTImageUriKey = "__ft_image_uri";
        private const string NFTImageUriKey = "__nft_image_uri";
        private const string NFTImageUrlKey = "__nft_image_url";

        public AElfClientProvider(IBlockchainClientFactory<AElfClient> blockchainClientFactory,
            ILogger<AElfClientProvider> logger)
        {
            _blockchainClientFactory = blockchainClientFactory;
            _logger = logger;
        }

        public string ChainType { get; } = "AElf";

        public async Task<long> GetBlockNumberAsync(string chainName)
        {
            var client = _blockchainClientFactory.GetClient(chainName);
            var chain = await client.GetChainStatusAsync();
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
            
            Log.Information($"get token info, chain: {chainName}, address:{address}, symbol: {symbol}, TokenInfo: {JsonConvert.SerializeObject(token)}");
            
            var externalInfo = token.ExternalInfo;
            if (externalInfo != null && externalInfo.Value != null)
            {
                if (externalInfo.Value.ContainsKey(FTImageUriKey))
                {
                    return new TokenDto
                    {
                        Address = address,
                        Decimals = token.Decimals,
                        Symbol = token.Symbol,
                        ImageUri = externalInfo.Value[FTImageUriKey]
                    };
                }
                if (externalInfo.Value.ContainsKey(NFTImageUriKey))
                {
                    return new TokenDto
                    {
                        Address = address,
                        Decimals = token.Decimals,
                        Symbol = token.Symbol,
                        ImageUri = externalInfo.Value[NFTImageUriKey]
                    };
                }
                if (externalInfo.Value.ContainsKey(NFTImageUrlKey))
                {
                    return new TokenDto
                    {
                        Address = address,
                        Decimals = token.Decimals,
                        Symbol = token.Symbol,
                        ImageUri = externalInfo.Value[NFTImageUrlKey]
                    };
                }
            }
            
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
            var transactionGetToken =
                await client.GenerateTransactionAsync(client.GetAddressFromPrivateKey(ChainsInitOptions.PrivateKey), address,
                    "GetTokenInfo",
                    paramGetBalance);
            var txWithSignGetToken = client.SignTransaction(ChainsInitOptions.PrivateKey, transactionGetToken);
            var transactionGetTokenResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSignGetToken.ToByteArray().ToHex()
            });
           
            return
                TokenInfo.Parser.ParseFrom(
                    ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));
        }

        public async Task<long> GetTransactionFeeAsync(string chainName, string transactionId)
        {
            try
            {
                var client = _blockchainClientFactory.GetClient(chainName);
                var result = await client.GetTransactionResultAsync(transactionId);
                if (result == null)
                {
                    return 0;
                }
                var transactionFeeCharged = TransactionFeeCharged.Parser.
                    ParseFrom(ByteString.FromBase64(result.Logs.First(l => l.Name == nameof(TransactionFeeCharged)).NonIndexed));
                return transactionFeeCharged.Amount;
            }
            catch (Exception e)
            {
                Log.Error(e, "GetTransactionFeeAsync fail.");
                return 0;
            }
        }

        public async Task<GetBalanceOutput> GetBalanceAsync(string chainName, string address,
            string contractAddress, string symbol)
        {
            var client = _blockchainClientFactory.GetClient(chainName);
            var paramGetBalance = new GetBalanceInput()
            {
                Symbol = symbol,
                Owner = new AElf.Client.Proto.Address()
                {
                    Value = AElf.Types.Address.FromBase58(address).Value
                }
            };

            var from = client.GetAddressFromPrivateKey(ChainsInitOptions.PrivateKey);
            Log.Information($"GenerateTransactionAsync, key: {ChainsInitOptions.PrivateKey}, from: {from}, to: {contractAddress}");
            var transactionGetBalance =
                await client.GenerateTransactionAsync(from,
                    contractAddress,
                    "GetBalance",
                    paramGetBalance);
            Log.Information($"transactionGetBalance: {transactionGetBalance}, from: {from}, to: {contractAddress}");
            var txWithSignGetBalance = client.SignTransaction(ChainsInitOptions.PrivateKey, transactionGetBalance);
            var transactionGetTokenResult = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSignGetBalance.ToByteArray().ToHex()
            });

            return GetBalanceOutput.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(transactionGetTokenResult));
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
                Log.Error(e, "Get the current status of a transaction {chainName}:{transactionHash} fail.", chainName, transactionHash);
                return -1;
            }
        }
    }
}