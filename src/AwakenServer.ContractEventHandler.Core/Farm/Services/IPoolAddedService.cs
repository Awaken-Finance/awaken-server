using System;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities.Ef;
using AwakenServer.Price;
using AwakenServer.Tokens;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Farm.Services
{
    public class AddPoolInfo
    {
        public string ChainId { get; set; }
        public Guid FarmId { get; set; }
        public string ChainName { get; set; }
        public string PoolToken { get; set; }
        public int Pid { get; set; }
        public long LastRewardBlock { get; set; }
        public int PoolType { get; set; }
        public int AllocationPoint { get; set; }
    }

    public interface IPoolAddedService
    {
        Task PoolAddedAsync(AddPoolInfo addPoolInfo);
    }

    public class PoolAddedService : IPoolAddedService, ITransientDependency
    {
        private readonly IRepository<FarmPool> _poolRepository;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmRepository;
        private readonly ITokenAppService _tokenAppService;
        private readonly IFarmTokenProvider _farmTokenProvider;
        private readonly ILogger<PoolAddedService> _logger;
        private readonly IObjectMapper _objectMapper;

        public PoolAddedService(IRepository<FarmPool> poolRepository,
            IRepository<Farms.Entities.Ef.Farm> farmRepository, ITokenAppService tokenAppService,
            IFarmTokenProvider farmTokenProvider, ILogger<PoolAddedService> logger, IObjectMapper objectMapper)
        {
            _poolRepository = poolRepository;
            _farmRepository = farmRepository;
            _tokenAppService = tokenAppService;
            _farmTokenProvider = farmTokenProvider;
            _logger = logger;
            _objectMapper = objectMapper;
        }

        public async Task PoolAddedAsync(AddPoolInfo addPoolInfo)
        {
            var swapTokenSymbol = addPoolInfo.PoolToken;
            var (swapToken, token1, token2) =
                await GetPoolTokensInfoAsync(addPoolInfo.ChainId, swapTokenSymbol, addPoolInfo.ChainName);
            await _poolRepository.InsertAsync(new FarmPool
            {
                ChainId = addPoolInfo.ChainId,
                FarmId = addPoolInfo.FarmId,
                SwapTokenId = swapToken.Id,
                Token1Id = token1?.Id ?? Guid.Empty,
                Token2Id = token2?.Id ?? Guid.Empty,
                Pid = addPoolInfo.Pid,
                Weight = addPoolInfo.AllocationPoint,
                LastUpdateBlockHeight = addPoolInfo.LastRewardBlock,
                PoolType = (PoolType) addPoolInfo.PoolType,
                AccumulativeDividendProjectToken = "0",
                AccumulativeDividendUsdt = "0",
                TotalDepositAmount = "0"
            });
            if (addPoolInfo.AllocationPoint > 0)
            {
                var farmEntity = await _farmRepository.GetAsync(x => x.Id == addPoolInfo.FarmId);
                farmEntity.TotalWeight += addPoolInfo.AllocationPoint;
                await _farmRepository.UpdateAsync(farmEntity);
            }
        }

        private async Task<(Token, Token, Token)> GetPoolTokensInfoAsync(string chainId, string poolToken,
            string chainName)
        {
            _logger.LogInformation($"query swapToken, chainID:{chainId}  swapToken: {poolToken}");
            var swapTokenDto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Symbol = poolToken
            });
            var swapToken = _objectMapper.Map<TokenDto, Token>(swapTokenDto);
            CheckTokenIsNull(chainName, poolToken, swapToken);

            var farmToken = _farmTokenProvider.GetFarmToken(chainName, poolToken);
            if (farmToken.Tokens == null || !farmToken.Tokens.Any())
            {
                _logger.LogInformation($"swapToken, chainID:{chainId}  swapToken: {poolToken} has not tokenOptions");
                return (swapToken, null, null);
            }

            var tokenOption1 = farmToken.Tokens[0];
            TokenOption tokenOption2 = null;
            if (farmToken.Tokens.Length > 1)
            {
                tokenOption2 = farmToken.Tokens[1];
            }

            var token1Dto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Symbol = tokenOption1.Symbol
            });
            var token1 = _objectMapper.Map<TokenDto, Token>(token1Dto);
            CheckTokenIsNull(chainName, poolToken, token1);

            _logger.LogInformation(
                $"Token0, chainID:{chainId}  Token0Address: {token1.Address} symbol: {token1.Symbol}");
            if (tokenOption2 == null)
            {
                return (swapToken, token1, null);
            }
            
            var token2Dto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Symbol = tokenOption2.Symbol
            });
            var token2 = _objectMapper.Map<TokenDto, Token>(token2Dto);
            CheckTokenIsNull(chainName, poolToken, token2);
            _logger.LogInformation(
                $"Token1, chainID:{chainId}  Token0Address: {token2.Address} symbol: {token2.Symbol}");
            return (swapToken, token1, token2);
        }

        private void CheckTokenIsNull(string chainName, string symbol, Token token)
        {
            if (token == null)
            {
                throw new Exception(
                    $"Lack token Information in db, symbol: {symbol} , chain name: {chainName}");
            }
        }
    }
}