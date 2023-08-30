using System;
using System.Threading.Tasks;
using AutoMapper.Internal.Mappers;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Xunit;

namespace AwakenServer.IDO
{
    public abstract class IDOTestBase : AwakenServerTestBase<IDOTestModule>
    {
        private readonly IChainAppService _chainAppService;
        protected readonly ITokenAppService TokenAppService;
        protected static string ChainId = Guid.NewGuid().ToString();
        protected readonly int AElfChainId = IDOTestConstants.AElfChainId;

        protected IDOTestBase()
        {
            _chainAppService = GetRequiredService<IChainAppService>();
            TokenAppService = GetRequiredService<ITokenAppService>();
            AsyncHelper.RunSync(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            var chainId = await InitializeChainAsync();
            await InitializeTokenAsync(chainId);
        }

        private async Task<string> InitializeChainAsync()
        {
            var chain = await _chainAppService.CreateAsync(new ChainCreateDto
            {
                Id = ChainId,
                AElfChainId = AElfChainId
            });
            //ChainId = chain.Id;
            return ChainId;
        }

        private async Task InitializeTokenAsync(string chainId)
        {
            foreach (var token in IDOTestConstants.Tokens)
            {
                token.ChainId = chainId;
                await TokenAppService.CreateAsync(new TokenCreateDto
                {
                    ChainId = token.ChainId,
                    Address = token.Address,
                    Decimals = token.Decimals,
                    Symbol = token.Symbol
                });
            }
        }
    }
}