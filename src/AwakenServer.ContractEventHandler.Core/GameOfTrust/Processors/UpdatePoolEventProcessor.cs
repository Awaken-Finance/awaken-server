using System;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.GameOfTrust.Dtos;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Tokens;
using Microsoft.Extensions.Options;
using Nethereum.Util;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.GameOfTrust.Processors
{
    public class UpdatePoolEventProcessor : EthereumEthereumEventProcessorBase<UpdatePoolEventDto>
    {
        private readonly IRepository<Entities.GameOfTrust.Ef.GameOfTrust> _repository;
        private readonly IChainAppService _chainAppService;
        private readonly ITokenProvider _tokenProvider;
        private readonly AnchorCoinsOptions _coinsOptions;

        public UpdatePoolEventProcessor(IRepository<Entities.GameOfTrust.Ef.GameOfTrust> repository,
            IChainAppService chainAppService,
            ITokenProvider tokenProvider, 
            IOptionsSnapshot<AnchorCoinsOptions> coinsOptions)
        {
            _repository = repository;
            _chainAppService = chainAppService;
            _tokenProvider = tokenProvider;
            _coinsOptions = coinsOptions.Value;
        }


        protected override async Task HandleEventAsync(UpdatePoolEventDto eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;

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

            string contractAddress = contractEventDetailsDto.Address;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var gameOfTrust = await _repository.FindAsync(x => x.Address == contractAddress
                                                              && x.Pid == eventDetailsEto.Pid
                                                              && x.ChainId == chain.Id);
            var depositToken = _tokenProvider.GetToken(gameOfTrust.DepositTokenId);
            gameOfTrust.UnlockMarketCap = (BigDecimal.Parse(eventDetailsEto.MarketCap.ToString()) /
                                           BigInteger.Pow(10, coin.Decimal)).ToString();
            gameOfTrust.RewardRate = eventDetailsEto.RewardRate.ToString();
            gameOfTrust.UnlockCycle = eventDetailsEto.UnlockCycle;
            gameOfTrust.TotalAmountLimit = (BigDecimal.Parse(eventDetailsEto.TotalAmountLimit.ToString()) /
                                            BigInteger.Pow(10, depositToken.Decimals)).ToString();
            await _repository.UpdateAsync(gameOfTrust);
        }
    }
}