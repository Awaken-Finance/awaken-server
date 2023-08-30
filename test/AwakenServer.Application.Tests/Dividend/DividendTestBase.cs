using System;
using System.Threading.Tasks;
using AutoMapper.Internal.Mappers;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;

namespace AwakenServer.Dividend
{
    public class DividendTestBase : AwakenServerTestBase<DividendTestModule>
    {
        //protected readonly IRepository<Chain> ChainsRepository;
        private readonly IChainAppService _chainAppService;
        protected readonly ITokenAppService TokenAppService;
        protected readonly IRepository<Entities.Dividend> DividendRepository;
        protected static string ChainId = Guid.NewGuid().ToString();
        protected Guid DividendId;
        protected readonly int AElfChainId = DividendTestConstants.AElfChainId;

        protected DividendTestBase()
        {
            _chainAppService = GetRequiredService<IChainAppService>();
            TokenAppService = GetRequiredService<ITokenAppService>();
            DividendRepository = GetRequiredService<IRepository<Entities.Dividend>>();
            AsyncHelper.RunSync(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            var chainId = await InitializeChainAsync();
            await InitializeTokenAsync(chainId);
            await InitializeDividendAsync(chainId);

            await _chainAppService.UpdateAsync(new ChainUpdateDto
            {
                Id = chainId,
                LatestBlockHeight = DividendTestConstants.CurrentBlockHeight,
            });
            
            /*
            var chainStatusCache = GetRequiredService<IDistributedCache<ChainStatusItem>>();
            var chainStatusItem = new ChainStatusItem
            {
                LatestBlockHeight = DividendTestConstants.CurrentBlockHeight
            };
            chainStatusCache.Set(chainId.ToString(), chainStatusItem);*/
            
        }

        private async Task<string> InitializeChainAsync()
        {
            var chain = await _chainAppService.CreateAsync(new ChainCreateDto
            {
                Id = ChainId,
                Name = "AElf",
                AElfChainId = AElfChainId
            });
            //ChainId = chain.Id;
            return ChainId;
        }

        private async Task InitializeDividendAsync(string chainId)
        {
            var dividend = await DividendRepository.InsertAsync(new Entities.Dividend
            {
                Address = DividendTestConstants.DividendTokenAddress,
                ChainId = chainId,
                TotalWeight = 0
            }, true);
            DividendId = dividend.Id;
        }

        private async Task InitializeTokenAsync(string chainId)
        {
            foreach (var token in DividendTestConstants.Tokens)
            {
                token.ChainId = chainId;
                await TokenAppService.CreateAsync(new TokenCreateDto
                {
                    ChainId = token.ChainId,
                    Symbol = token.Symbol,
                    Address = token.Address,
                    Decimals = token.Decimals
                });
            }
        }
    }
}