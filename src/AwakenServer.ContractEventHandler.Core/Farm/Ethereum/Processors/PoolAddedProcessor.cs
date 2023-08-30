using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Farm.Ethereum.DTOs;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using AwakenServer.Price;
using AwakenServer.Tokens;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Farm.Ethereum.Processors
{
    public class PoolAddedProcessor : EthereumEthereumEventProcessorBase<PoolAdded>
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly ITokenProvider _tokenProvider;
        private readonly IFarmTokenProvider _farmTokenProvider;
        private readonly ILogger<PoolAddedProcessor> _logger;
        private readonly IObjectMapper _objectMapper;

        public PoolAddedProcessor(ILogger<PoolAddedProcessor> logger,
            IRepository<FarmPool> poolRepository, ITokenProvider tokenProvider,IObjectMapper objectMapper,
            IRepository<Farms.Entities.Ef.Farm> farmRepository, ICommonInfoCacheService commonInfoCacheService, IFarmTokenProvider farmTokenProvider)
        {
            _logger = logger;
            _poolRepository = poolRepository;
            _tokenProvider = tokenProvider;
            _farmRepository = farmRepository;
            _commonInfoCacheService = commonInfoCacheService;
            _farmTokenProvider = farmTokenProvider;
            _objectMapper = objectMapper;
        }

        protected override async Task HandleEventAsync(
            PoolAdded eventDetailsEto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            _logger.LogInformation($"query chain, node name : {nodeName}");
            var (chain, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(nodeName, contractEventDetailsDto.Address);
            var swapTokenAddress = eventDetailsEto.SwapToken;
            var (swapToken, token1, token2) = await GetPoolTokensInfoAsync(chain, swapTokenAddress);
            await _poolRepository.InsertAsync(new FarmPool
            {
                ChainId = chain.Id,
                FarmId = farm.Id,
                SwapTokenId = swapToken.Id,
                Token1Id = token1?.Id ?? Guid.Empty,
                Token2Id = token2?.Id ?? Guid.Empty,
                Pid = eventDetailsEto.Pid,
                Weight = eventDetailsEto.AllocationPoint,
                LastUpdateBlockHeight = eventDetailsEto.LastRewardBlockHeight,
                PoolType = (PoolType) eventDetailsEto.PoolType,
                AccumulativeDividendProjectToken = "0",
                AccumulativeDividendUsdt = "0",
                TotalDepositAmount = "0"
            });
            if (eventDetailsEto.AllocationPoint > 0)
            {
                var farmEntity = await _farmRepository.GetAsync(x => x.Id == farm.Id);
                farmEntity.TotalWeight += eventDetailsEto.AllocationPoint;
                await _farmRepository.UpdateAsync(farmEntity);
            }
        }

        private async Task<(Token, Token, Token)> GetPoolTokensInfoAsync(Chain chain, string swapTokenAddress)
        {
            _logger.LogInformation($"query swapToken, chainID:{chain.Id}  swapTokenAddress: {swapTokenAddress}");
            var swapToken =
                _objectMapper.Map<TokenDto, Token>(
                    await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, swapTokenAddress));
            var farmToken = _farmTokenProvider.GetFarmToken(chain.Name, swapTokenAddress);
            if (farmToken.Tokens == null || !farmToken.Tokens.Any())
            {
                _logger.LogInformation($"swapToken, chainID:{chain.Id}  swapTokenAddress: {swapTokenAddress} symbol: {swapToken.Symbol} has not tokenOptions");
                return (swapToken, null, null);
            }

            var tokenOption1 =  farmToken.Tokens[0];
            TokenOption tokenOption2 = null;
            if (farmToken.Tokens.Length > 1)
            {
                tokenOption2 = farmToken.Tokens[1];
            }
            
            var token1 = _objectMapper.Map<TokenDto, Token>(await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name,tokenOption1.Address));
            _logger.LogInformation($"Token0, chainID:{chain.Id}  Token0Address: {token1.Address} symbol: {token1.Symbol}");
            if (tokenOption2 == null)
            {
                return (swapToken, token1, null);
            }
            var token2 = _objectMapper.Map<TokenDto, Token>(await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name,tokenOption2.Address));
            _logger.LogInformation($"Token1, chainID:{chain.Id}  Token0Address: {token2.Address} symbol: {token2.Symbol}");
            return (swapToken, token1, token2);
        }
    }
}