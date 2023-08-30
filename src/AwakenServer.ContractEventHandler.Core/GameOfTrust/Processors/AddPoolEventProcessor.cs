using System;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using AwakenServer.ContractEventHandler.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Processors
{
    public class AddPoolEventProcessor : EthereumEthereumEventProcessorBase<AddPoolEventDto>
    {
        private readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> _repository;
        private readonly IChainAppService _chainAppService;
        private readonly ILogger<AddPoolEventProcessor> _logger;
        private readonly AnchorCoinsOptions _coinsOptions;
        private readonly ITokenProvider _tokenProvider;


        public AddPoolEventProcessor(IRepository<Entities.GameOfTrust.Ef.GameOfTrust> repository,
            IChainAppService chainAppService,
            ILogger<AddPoolEventProcessor> logger, IOptionsSnapshot<AnchorCoinsOptions> coinsOptions, ITokenProvider tokenProvider)
        {
            _repository = repository;
            _chainAppService = chainAppService;
            _logger = logger;
            _tokenProvider = tokenProvider;
            _coinsOptions = coinsOptions.Value;
        }

        protected override async Task HandleEventAsync(AddPoolEventDto eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            _logger.LogInformation("Income message:", eventDetailsEto.ToString());
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var contractAddress = contractEventDetailsDto.Address;
            var depositToken = await _tokenProvider.GetOrAddTokenAsync(chain.Id, nodeName, eventDetailsEto.DepositToken);
            var harvestToken = await _tokenProvider.GetOrAddTokenAsync(chain.Id, nodeName, eventDetailsEto.HarvestToken);
            AnchorCoin coin = null;
            foreach (var anchorCoin in _coinsOptions.AnchorCoinsList)
            {
                if (anchorCoin.Chain.Equals(nodeName))
                {
                    coin = anchorCoin;
                }
            }

            if (coin == null)
            {
                throw new Exception("AnchorCoin config wrong.");
            }

            await _repository.InsertAsync(new Entities.GameOfTrust.Ef.GameOfTrust
            {
                ChainId = chain.Id,
                Pid = eventDetailsEto.Pid,
                Address = contractAddress,
                RewardRate = eventDetailsEto.RewardRate.ToString(),
                UnlockCycle = eventDetailsEto.UnlockCycle,
                UnlockHeight = 0,
                StartHeight = eventDetailsEto.StartBlock,
                EndHeight = eventDetailsEto.StakeEndBlock,
                BlocksDaily = eventDetailsEto.BlocksDaily,
                DepositTokenId = depositToken.Id,
                HarvestTokenId = harvestToken.Id,
                TotalAmountLimit =
                    (BigDecimal.Parse(eventDetailsEto.TotalAmountLimit.ToString()) /
                     BigInteger.Pow(10, depositToken.Decimals)).ToString(),
                FineAmount = "0",
                UnlockMarketCap =
                    (BigDecimal.Parse(eventDetailsEto.MarketCap.ToString()) / BigInteger.Pow(10, coin.Decimal))
                    .ToString(),
                TotalValueLocked = "0",
            });
        }

        // private async Task<Token> GetOrAddTokenAsync(string tokenKey, Guid chainId, string address)
        // {
        //     var token = await _tokenInfoProvider.GetOrSetCachedDataAsync(tokenKey,
        //         x => x.Address == address && x.ChainId == chainId);
        //     if (token == null)
        //     {
        //         var chain = _chainInfoProvider.GetCachedDataById(chainId);
        //         var web3 = new Nethereum.Web3.Web3(_apiOptions.ChainNodeApis[chain.Name]);
        //         var contractHandler = web3.Eth.GetContractHandler(address);
        //         var symbol = await contractHandler.QueryAsync<SymbolFunction, string>();
        //         var decimals = await contractHandler.QueryAsync<DecimalsFunction, int>();
        //
        //         token = await _tokenRepository.InsertAsync(new Token
        //         {
        //             Address = address,
        //             Decimals = decimals,
        //             Symbol = symbol,
        //             ChainId = chainId
        //         });
        //         _tokenInfoProvider.SetCachedData(tokenKey, token.Id, token);
        //         await _tokenRepository.InsertAsync(token);
        //     }
        //
        //     return token;
        // }
    }
}