using System;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Farms.Options;
using AwakenServer.Tokens;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Services
{
    public class FarmTokenInitializeService : ITransientDependency
    {
        private readonly FarmTokenOptions _farmTokenOptions;
        private readonly FarmOption _farmOption;
        private readonly TokenAppService _tokenAppService;
        private readonly IChainAppService _chainAppService;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmsRepository;

        public FarmTokenInitializeService(IOptionsSnapshot<FarmTokenOptions> farmTokenOptions,
            IOptionsSnapshot<FarmOption> farmOptions, TokenAppService tokenAppService,
            IChainAppService chainAppService, IRepository<Farms.Entities.Ef.Farm> farmsRepository)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _farmsRepository = farmsRepository;
            _farmTokenOptions = farmTokenOptions.Value;
            _farmOption = farmOptions.Value;
        }

        public async Task InitializeFarmTokenAsync()
        {
            if (_farmTokenOptions.FarmTokens != null)
            {
                foreach (var farmToken in _farmTokenOptions.FarmTokens.Values)
                {
                    await EnsureTokenExistAsync(farmToken);
                }
            }

            if (_farmOption.IsResetData)
            {
                await EnsureFarmExisted();
            }
        }

        private async Task EnsureTokenExistAsync(FarmToken farmToken)
        {
            var chain = await _chainAppService.GetByNameCacheAsync(farmToken.ChainName);
            var baseToken =
                await _tokenAppService.GetAsync(new GetTokenInput
                {
                    Symbol = farmToken.Symbol,
                    Address = farmToken.Address,
                    ChainId = chain.Id,
                });
            if (baseToken == null)
            {
                await _tokenAppService.CreateAsync(GetNewToken(chain.Id, farmToken.Address, farmToken.Symbol,
                    farmToken.Decimals));
            }

            if (farmToken.Tokens == null || !farmToken.Tokens.Any())
            {
                return;
            }

            foreach (var tokenOption in farmToken.Tokens)
            {
                var token = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    Symbol = tokenOption.Symbol,
                    Address = tokenOption.Address,
                    ChainId = chain.Id,
                });//x =>
                if (token != null)
                {
                    continue;
                }

                await _tokenAppService.CreateAsync(GetNewToken(chain.Id, tokenOption.Address, tokenOption.Symbol,
                    tokenOption.Decimals));
            }
        }

        private async Task EnsureFarmExisted()
        {
            foreach (var fp in _farmOption.Farms)
            {
                var targetFarm =
                    await _farmsRepository.FindAsync(f => f.FarmAddress == fp.FarmAddress && f.ChainId == fp.ChainId);
                if (targetFarm != null)
                {
                    return;
                }

                await _farmsRepository.InsertAsync(new Farms.Entities.Ef.Farm
                {
                    ChainId = fp.ChainId,
                    FarmAddress = fp.FarmAddress,
                    FarmType = fp.FarmType,
                    StartBlock = fp.StartBlock,
                    MiningHalvingPeriod1 = fp.MiningHalvingPeriod1,
                    MiningHalvingPeriod2 = fp.MiningHalvingPeriod2,
                    UsdtDividendPerBlock = fp.UsdtDividendPerBlock,
                    UsdtDividendStartBlockHeight = fp.UsdtDividendStartBlockHeight,
                    UsdtDividendEndBlockHeight = fp.UsdtDividendEndBlockHeight,
                    ProjectTokenMinePerBlock1 = fp.ProjectTokenMinePerBlock1,
                    ProjectTokenMinePerBlock2 = fp.ProjectTokenMinePerBlock2,
                    TotalWeight = fp.TotalWeight
                });
            }
        }

        private TokenCreateDto GetNewToken(string chainId, string address, string symbol, int decimals)
        {
            return new()
            {
                ChainId = chainId,
                Address = address,
                Symbol = symbol,
                Decimals = decimals
            };
        }
    }
}