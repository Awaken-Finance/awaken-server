using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.Tests;
using AwakenServer.SwapTokenPath;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Org.BouncyCastle.Crypto.Prng.Drbg;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.SwapPath
{
    [Collection(ClusterCollection.Name)]
    public class TokenPathAppServiceTests : TradeTestBase
    {
        private readonly ITokenPathAppService _tokenPathAppAppService;

        public TokenPathAppServiceTests(ITokenPathAppService tokenPathAppAppService)
        {
            _tokenPathAppAppService = tokenPathAppAppService;
        }
        
    }
}