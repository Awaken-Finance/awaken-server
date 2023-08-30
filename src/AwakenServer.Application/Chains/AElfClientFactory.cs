using System.Collections.Concurrent;
using AElf.Client.Service;
using Microsoft.Extensions.Options;

namespace AwakenServer.Chains
{
    public class AElfClientFactory : IBlockchainClientFactory<AElfClient>
    {
        private readonly ApiOptions _apiOptions;
        private readonly ConcurrentDictionary<string, AElfClient> _clientDic;

        public AElfClientFactory(IOptionsSnapshot<ApiOptions> apiOptions)
        {
            _apiOptions = apiOptions.Value;
            _clientDic = new ConcurrentDictionary<string, AElfClient>();
        }

        public AElfClient GetClient(string chainName)
        {
            if (_clientDic.TryGetValue(chainName, out var client))
            {
                return client;
            }

            client = new AElfClient(_apiOptions.ChainNodeApis[chainName]);
            _clientDic[chainName] = client;
            return client;
        }
    }
}