using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Provider;
using AElf.Indexing.Elasticsearch.Services;
using AwakenServer.Debits.Entities.Es;
using AwakenServer.Debits.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.EntityHandler.Services
{
    public class DebitInitializeService : ITransientDependency
    {
        private readonly DebitOption _debitOption;
        private readonly IElasticIndexService _elasticIndexService;
        private readonly IEsClientProvider _esClientProvider;
        private readonly INESTRepository<CompController, Guid> _nestRepository;

        public DebitInitializeService(
            IOptions<DebitOption> debitOption, IElasticIndexService elasticIndexService,
            IEsClientProvider esClientProvider, INESTRepository<CompController, Guid> nestRepository)
        {
            _debitOption = debitOption.Value;
            _elasticIndexService = elasticIndexService;
            _esClientProvider = esClientProvider;
            _nestRepository = nestRepository;
        }

        public async Task InitializeDataAsync()
        {
            if (!_debitOption.IsResetData)
            {
                return;
            }

            //await RebuildDebitIndexAsync();
            //await InitializeCompController();
        }

        private async Task RebuildDebitIndexAsync()
        {
            await RebuildIndexAsync<CompController>();
            await RebuildIndexAsync<CToken>();
            await RebuildIndexAsync<CTokenUserInfo>();
            await RebuildIndexAsync<CTokenRecord>();
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

        private async Task InitializeCompController()
        {
            foreach (var compController in _debitOption.CompControllerList)
            {
                await _nestRepository.AddOrUpdateAsync(compController.GetCompController());
            }
        }
    }
}