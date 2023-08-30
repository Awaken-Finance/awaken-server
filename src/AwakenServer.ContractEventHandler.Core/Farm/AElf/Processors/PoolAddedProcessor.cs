using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using Awaken.Contracts.Farm;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Farm.Services;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using AwakenServer.Price;
using AwakenServer.Tokens;
using Volo.Abp.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AwakenServer.ContractEventHandler.Farm.AElf.Processors
{
    public class PoolAddedProcessor : AElfEventProcessorBase<PoolAdded>
    {
        private readonly ICommonInfoCacheService _commonInfoCacheService;
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;
        private readonly ITokenProvider _tokenProvider;
        private readonly IFarmTokenProvider _farmTokenProvider;
        private readonly ILogger<PoolAddedProcessor> _logger;

        public PoolAddedProcessor(ICommonInfoCacheService commonInfoCacheService,
            IRepository<FarmPool> poolRepository,
            IRepository<Farms.Entities.Ef.Farm> farmRepository,
            IFarmTokenProvider farmTokenProvider, ILogger<PoolAddedProcessor> logger,
            ITokenProvider tokenProvider)
        {
            _commonInfoCacheService = commonInfoCacheService;
            _poolRepository = poolRepository;
            _farmRepository = farmRepository;
            _farmTokenProvider = farmTokenProvider;
            _logger = logger;
            _tokenProvider = tokenProvider;
        }

        protected override async Task HandleEventAsync(PoolAdded eventDetailsEto, EventContext txInfoDto)
        {
            var (chain, farm) =
                await _commonInfoCacheService.GetCommonCacheInfoAsync(aelfChainId: txInfoDto.ChainId,
                    farmAddress: txInfoDto.EventAddress);
            var (swapTokenId, token1Id, token2Id) =
                await GetPoolTokensInfoAsync(chain, eventDetailsEto.Token);
            var pool = await _poolRepository.FindAsync(x => x.FarmId == farm.Id && x.Pid == (int) eventDetailsEto.Pid);
            if (pool != null)
            {
                return;
            }
            
            await _poolRepository.InsertAsync(new FarmPool
            {
                ChainId = chain.Id,
                FarmId = farm.Id,
                SwapTokenId = swapTokenId,
                Token1Id = token1Id ?? Guid.Empty,
                Token2Id = token2Id ?? Guid.Empty,
                Pid = (int) eventDetailsEto.Pid,
                Weight = (int) eventDetailsEto.AllocationPoint,
                LastUpdateBlockHeight = eventDetailsEto.LastRewardBlockHeight,
                PoolType = (PoolType) eventDetailsEto.PoolType,
                AccumulativeDividendProjectToken = "0",
                AccumulativeDividendUsdt = "0",
                TotalDepositAmount = "0"
            }, true);
            if (eventDetailsEto.AllocationPoint > 0)
            {
                var farmEntity = await _farmRepository.GetAsync(x => x.Id == farm.Id);
                farmEntity.TotalWeight += (int) eventDetailsEto.AllocationPoint;
                await _farmRepository.UpdateAsync(farmEntity);
            }
        }

        private async Task<(Guid, Guid?, Guid?)> GetPoolTokensInfoAsync(Chain chain,
            string poolToken)
        {
            _logger.LogInformation($"query swapToken, chainID:{chain.Id}  swapToken: {poolToken}");
            var swapToken = await GetTokenAsync(chain, poolToken);
            var farmToken = _farmTokenProvider.GetFarmToken(chain.Name, poolToken);
            if (farmToken.Tokens == null || !farmToken.Tokens.Any())
            {
                _logger.LogInformation($"swapToken, chainID:{chain.Id}  swapToken: {poolToken} has not tokenOptions");
                return (swapToken.Id, null, null);
            }

            var tokenOption1 = farmToken.Tokens[0];
            TokenOption tokenOption2 = null;
            if (farmToken.Tokens.Length > 1)
            {
                tokenOption2 = farmToken.Tokens[1];
            }

            var token1 = await GetTokenAsync(chain, tokenOption1.Symbol, tokenOption1.Address);
            _logger.LogInformation(
                $"Token0, chainID:{chain.Id}  Token0Address: {token1.Address} symbol: {token1.Symbol}");
            if (tokenOption2 == null)
            {
                return (swapToken.Id, token1.Id, null);
            }

            var token2 = await GetTokenAsync(chain, tokenOption2.Symbol, tokenOption2.Address);
            _logger.LogInformation(
                $"Token1, chainID:{chain.Id}  Token0Address: {token2.Address} symbol: {token2.Symbol}");
            return (swapToken.Id, token1.Id, token2.Id);
        }

        private async Task<TokenDto> GetTokenAsync(Chain chain, string symbol,
            string address = null)
        {
            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, address, symbol);
            if (token == null)
            {
                throw new Exception(
                    $"Lack token Information in db, symbol: {symbol} , chain name: {chain.AElfChainId}");
            }

            return token;
        }
    }
}