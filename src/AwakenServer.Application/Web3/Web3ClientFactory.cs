using System.Collections.Concurrent;
using AwakenServer.Chains;
using Microsoft.Extensions.Options;

namespace AwakenServer.Web3
{
    public class Web3ClientFactory : IBlockchainClientFactory<Nethereum.Web3.Web3>
    {
        private readonly ApiOptions _apiOptions;

        private readonly ConcurrentDictionary<string, Nethereum.Web3.Web3> _web3Dic;

        public Web3ClientFactory(IOptionsSnapshot<ApiOptions> apiOptions)
        {
            _apiOptions = apiOptions.Value;
            _web3Dic = new ConcurrentDictionary<string, Nethereum.Web3.Web3>();
        }

        public Nethereum.Web3.Web3 GetClient(string chainName)
        {
            if (_web3Dic.TryGetValue(chainName, out var web3))
            {
                return web3;
            }

            web3 = new Nethereum.Web3.Web3(_apiOptions.ChainNodeApis[chainName]);
            _web3Dic[chainName] = web3;
            return web3;
        }
    }
}