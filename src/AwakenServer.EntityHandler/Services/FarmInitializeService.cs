using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Provider;
using AElf.Indexing.Elasticsearch.Services;
using AwakenServer.Farms.Entities.Es;
using AwakenServer.Farms.Options;
using AwakenServer.Tokens;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.EntityHandler.Services
{
    public class FarmInitializeService : ITransientDependency
    {
        private readonly FarmOption _farmOption;
        private readonly INESTRepository<Farm, Guid> _farmRepository;
        private readonly INESTRepository<FarmPool, Guid> _farmPoolRepository;
        private readonly IElasticIndexService _elasticIndexService;
        private readonly IEsClientProvider _esClientProvider;

        public FarmInitializeService(IOptions<FarmOption> farmOption, INESTRepository<Farm, Guid> farmRepository,
            IElasticIndexService elasticIndexService,
            IEsClientProvider esClientProvider, INESTRepository<FarmPool, Guid> farmPoolRepository)
        {
            _farmRepository = farmRepository;
            _elasticIndexService = elasticIndexService;
            _esClientProvider = esClientProvider;
            _farmPoolRepository = farmPoolRepository;
            _farmOption = farmOption.Value;
        }

        public async Task InitializeDataAsync()
        {
            if (!_farmOption.IsResetData)
            {
                return;
            }

            //await RebuildFarmIndexAsync();
        }

        private async Task RebuildIndexAsync<T>()
        {
            var indexName = typeof(T).Name.ToLower();
            var client = _esClientProvider.GetClient();
            var exits = await client.Indices.ExistsAsync(indexName);
            if (exits.Exists)
            {
                await _elasticIndexService.DeleteIndexAsync(indexName);
            }

            await _elasticIndexService.CreateIndexAsync(indexName, typeof(T));
        }

        private async Task RebuildFarmIndexAsync()
        {
            await RebuildIndexAsync<Farm>();
            await RebuildIndexAsync<FarmPool>();
            await RebuildIndexAsync<FarmRecord>();
            await RebuildIndexAsync<FarmUserInfo>();
        }

        private async Task InitializeFarmAsync()
        {
            foreach (var farm in _farmOption.Farms)
            {
                await _farmRepository.AddAsync(farm.GetFarm());
            }
        }

        private async Task MockDataAsync()
        {
            var pools = (await _farmPoolRepository.GetListAsync()).Item2;
            var pool = pools.SingleOrDefault(x => x.Id == Guid.Parse("3a0035b3-c6f5-84f0-82ec-0a6289246fe4"));
            pool.Token1 = new Token()
            {
                Id = Guid.NewGuid(),
                Decimals = 18,
                Symbol = "USDT",
                Address = "0x3F280eE5876CE8B15081947E0f189E336bb740A5",
                ChainId = pool.ChainId
            };

            pool.Token2 = new Token()
            {
                Id = Guid.NewGuid(),
                Decimals = 18,
                Symbol = "ETH",
                Address = "0x86b8AC6E084B8fF4E851716Ca8c3F8E5BAdb099e",
                ChainId = pool.ChainId
            };
            await _farmPoolRepository.UpdateAsync(pool);

            pool = pools.SingleOrDefault(x => x.Id == Guid.Parse("3a0035b3-df26-20f9-cc6b-0f93a14163f6"));
            pool.Token1 = new Token()
            {
                Id = Guid.NewGuid(),
                Decimals = 18,
                Symbol = "ISTAR",
                Address = "0x20d94fb914beE2F04C196b8E9E1F4fd7858348C7",
                ChainId = pool.ChainId
            };

            pool.Token2 = new Token()
            {
                Id = Guid.NewGuid(),
                Decimals = 18,
                Symbol = "WBNB",
                Address = "0xae13d989daC2f0dEbFf460aC112a837C89BAa7cd",
                ChainId = pool.ChainId
            };
            await _farmPoolRepository.UpdateAsync(pool);

            pool = pools.SingleOrDefault(x => x.Id == Guid.Parse("3a0035b3-e15b-aedc-8a41-8875c31b8b9d"));
            pool.Token1 = new Token()
            {
                Id = Guid.NewGuid(),
                Decimals = 18,
                Symbol = "GLP",
                Address = "0xe543A56983126711990bE97392dceD0adB2612cA",
                ChainId = pool.ChainId
            };

            pool.Token2 = new Token()
            {
                Id = Guid.NewGuid(),
                Decimals = 18,
                Symbol = "WBNB",
                Address = "0xae13d989daC2f0dEbFf460aC112a837C89BAa7cd",
                ChainId = pool.ChainId
            };
            await _farmPoolRepository.UpdateAsync(pool);
        }
    }
}