using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Farms;
using AwakenServer.Tokens;
using Volo.Abp.Caching;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using EsFarm = AwakenServer.Farms.Entities.Es.Farm;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Farm
{
    public abstract class AwakenServerFarmApplicationTestBase : AwakenServerTestBase<AwakenServerFarmTestModule>
    {
        //private readonly IRepository<Chain> _chainsRepository;
        //private readonly INESTRepository<Chain, Guid> _chainIndexRepository;
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly IRepository<Farms.Entities.Ef.Farm> _farmsRepository;
        protected static string DefaultChainId = Guid.NewGuid().ToString();
        protected int DefaultChainAElfId;

        protected AwakenServerFarmApplicationTestBase()
        {
            _chainAppService = GetRequiredService<IChainAppService>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
            _farmsRepository = GetRequiredService<IRepository<Farms.Entities.Ef.Farm>>();
            AsyncHelper.RunSync(async () => await SeedAsync());

            var chainAppService = GetRequiredService<IChainAppService>();
            chainAppService.UpdateAsync(new ChainUpdateDto
            {
                Id = DefaultChainId,
                LatestBlockHeight = 2000,
            });
        }

        public async Task SeedAsync()
        {
            /* Seed additional test data... */
            await InitializeChainAndTokenInfoAsync();
            await InitializeFarmInfoAsync();
        }

        private async Task InitializeChainAndTokenInfoAsync()
        {
            var defaultChain = await _chainAppService.CreateAsync(new ChainCreateDto
            {
                Id = DefaultChainId,
                Name = FarmTestData.DefaultNodeName,
                AElfChainId = FarmTestData.DefaultAElfChainId
            });
            //DefaultChainId = defaultChain.Id;
            DefaultChainAElfId = defaultChain.AElfChainId;

            var swapOne = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenOneContractAddress,
                Symbol = FarmTestData.SwapTokenOneSymbol,
                Decimals = FarmTestData.SwapTokenOneDecimal
            });

            var swapToken1 = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenOneToken1ContractAddress,
                Symbol = FarmTestData.SwapTokenOneToken1Symbol,
                Decimals = FarmTestData.SwapTokenOneToken1Decimal
            });

            var swapToken2 = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenOneToken2ContractAddress,
                Symbol = FarmTestData.SwapTokenOneToken2Symbol,
                Decimals = FarmTestData.SwapTokenOneToken2Decimal
            });

            var swapTwo = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenTwoContractAddress,
                Symbol = FarmTestData.SwapTokenTwoSymbol,
                Decimals = FarmTestData.SwapTokenTwoDecimal
            });

            var swapTwoToken1 = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenTwoToken1ContractAddress,
                Symbol = FarmTestData.SwapTokenTwoToken1Symbol,
                Decimals = FarmTestData.SwapTokenTwoToken1Decimal
            });

            var swapTwoToken2 = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenTwoToken2ContractAddress,
                Symbol = FarmTestData.SwapTokenTwoToken2Symbol,
                Decimals = FarmTestData.SwapTokenTwoToken2Decimal
            });

            var swapThree = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenThreeContractAddress,
                Symbol = FarmTestData.SwapTokenThreeSymbol,
                Decimals = FarmTestData.SwapTokenThreeDecimal
            });

            var swapThreeToken1 = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenThreeToken1ContractAddress,
                Symbol = FarmTestData.SwapTokenThreeToken1Symbol,
                Decimals = FarmTestData.SwapTokenThreeToken1Decimal
            });

            var swapThreeToken2 = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenThreeToken2ContractAddress,
                Symbol = FarmTestData.SwapTokenThreeToken2Symbol,
                Decimals = FarmTestData.SwapTokenThreeToken2Decimal
            });

            var tokenToken = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.ProjectTokenContractAddress,
                Symbol = FarmTestData.ProjectTokenSymbol,
                Decimals = FarmTestData.ProjectTokenDecimal
            });

            var usdtToken = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.UsdtTokenContractAddress,
                Symbol = FarmTestData.UsdtTokenSymbol,
                Decimals = FarmTestData.UsdtTokenDecimal
            });
            
            var swapFour = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChainId,
                Address = FarmTestData.SwapTokenFourContractAddress,
                Symbol = FarmTestData.SwapTokenFourSymbol,
                Decimals = FarmTestData.SwapTokenFourDecimal
            });
        }

        private async Task InitializeFarmInfoAsync()
        {
            var massiveFarm = await _farmsRepository.InsertAsync(new Farms.Entities.Ef.Farm
            {
                FarmAddress = FarmTestData.MassiveFarmAddress,
                FarmType = FarmType.Massive,
                MiningHalvingPeriod1 = FarmTestData.MassiveFarmMiningHalvingPeriod1,
                MiningHalvingPeriod2 = FarmTestData.MassiveFarmMiningHalvingPeriod2,
                ProjectTokenMinePerBlock1 = FarmTestData.MassiveFarmProjectTokenMinePerBlock1,
                ProjectTokenMinePerBlock2 = FarmTestData.MassiveFarmProjectTokenMinePerBlock2,
                ChainId = DefaultChainId
            }, true);

            var generalFarm = await _farmsRepository.InsertAsync(new Farms.Entities.Ef.Farm
            {
                FarmAddress = FarmTestData.GeneralFarmAddress,
                FarmType = FarmType.Compound,
                StartBlock = FarmTestData.GeneralFarmStartBlock,
                MiningHalvingPeriod1 = FarmTestData.GeneralFarmMiningHalvingPeriod1,
                ProjectTokenMinePerBlock1 = FarmTestData.GeneralFarmProjectTokenMinePerBlock1,
                ChainId = DefaultChainId
            }, true);
        }
    }
}