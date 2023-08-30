using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Providers;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AwakenServer.ContractEventHandler.Farm.Services
{
    public interface ICommonInfoCacheService
    {
        Task<(Chain, Farms.Entities.Ef.Farm)> GetCommonCacheInfoAsync(string nodeName = null, string farmAddress = null,
            int? aelfChainId = null);
    }

    public class CommonInfoCacheService : ICommonInfoCacheService, ISingletonDependency
    {
        private readonly IChainAppService _chainAppService;
        private readonly ICachedDataProvider<Farms.Entities.Ef.Farm> _farmInfoProvider;
        private readonly ILogger<CommonInfoCacheService> _logger;
        private readonly IObjectMapper _objectMapper;

        public CommonInfoCacheService(IChainAppService chainAppService,
            ICachedDataProvider<Farms.Entities.Ef.Farm> farmInfoProvider, ILogger<CommonInfoCacheService> logger,
            IObjectMapper objectMapper)
        {
            _chainAppService = chainAppService;
            _farmInfoProvider = farmInfoProvider;
            _logger = logger;
            _objectMapper = objectMapper;
        }

        public async Task<(Chain, Farms.Entities.Ef.Farm)> GetCommonCacheInfoAsync(string nodeName = null,
            string farmAddress = null, int? aelfChainId = null)
        {
            if (!string.IsNullOrEmpty(nodeName))
            {
                return await GetEthereumCommonCacheInfoAsync(nodeName, farmAddress);
            }

            if (aelfChainId.HasValue)
            {
                return await GetAElfCacheInfoAsync(aelfChainId.Value, farmAddress);
            }

            throw new Exception("invalid input for searching chain");
        }

        private async Task<(Chain, Farms.Entities.Ef.Farm)> GetEthereumCommonCacheInfoAsync(string nodeName,
            string farmAddress = null)
        {
            var chainDto = await _chainAppService.GetByNameCacheAsync(nodeName);
            if (chainDto == null)
            {
                throw new Exception($"Fail to find chain info, node name: {nodeName}");
            }

            var chain = _objectMapper.Map<ChainDto, Chain>(chainDto);

            if (farmAddress == null)
            {
                return (chain, null);
            }

            _logger.LogInformation($"query farm, chainID:{chain.Id}  farmAddress: {farmAddress}");
            var farm = await _farmInfoProvider.GetOrSetCachedDataAsync(nodeName + farmAddress,
                x => x.FarmAddress == farmAddress && x.ChainId == chain.Id);
            if (farm == null)
            {
                throw new Exception($"Fail to find farm info, address: {farmAddress}");
            }

            return (chain, farm);
        }

        private async Task<(Chain, Farms.Entities.Ef.Farm)> GetAElfCacheInfoAsync(int chainId,
            string farmAddress = null)
        {
            var chainDto = await _chainAppService.GetByChainIdCacheAsync(chainId.ToString());
            if (chainDto == null)
            {
                throw new Exception($"Fail to find chain info, node name: {chainId}");
            }

            var chain = _objectMapper.Map<ChainDto, Chain>(chainDto);

            if (farmAddress == null)
            {
                return (chain, null);
            }

            _logger.LogInformation($"query farm, chainID:{chain.Id}  farmAddress: {farmAddress}");
            var farm = await _farmInfoProvider.GetOrSetCachedDataAsync(chainId + farmAddress,
                x => x.FarmAddress == farmAddress && x.ChainId == chain.Id);
            if (farm == null)
            {
                throw new Exception($"Fail to find farm info, address: {farmAddress}");
            }

            return (chain, farm);
        }
    }
}