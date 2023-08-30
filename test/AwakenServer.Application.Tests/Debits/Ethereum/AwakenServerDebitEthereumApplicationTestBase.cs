using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Chains;
using AwakenServer.Debits.Entities.Ef;
using AwakenServer.Tokens;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;
using EsCompController = AwakenServer.Debits.Entities.Es.CompController;

namespace AwakenServer.Debits.Ethereum
{
    public class AwakenServerDebitEthereumApplicationTestBase : AwakenServerTestBase<AwakenServerDebitTestModule>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly IRepository<CompController> _compControllersRepository;
        private readonly IRepository<CToken> _cTokensRepository;
        private readonly INESTRepository<Entities.Es.CompController, Guid> _esCompRepository;
        private readonly IObjectMapper _objectMapper;
        protected Chain DefaultChain;
        protected static string DefaultChainId = Guid.NewGuid().ToString();
        protected CompController CompController;
        protected CToken CProjectToken;
        protected CToken CTokenOne;

        public AwakenServerDebitEthereumApplicationTestBase()
        {
            _chainAppService = GetRequiredService<IChainAppService>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
            _compControllersRepository = GetRequiredService<IRepository<CompController>>();
            _cTokensRepository = GetRequiredService<IRepository<CToken>>();
            _esCompRepository = GetRequiredService<INESTRepository<Entities.Es.CompController, Guid>>();
            _objectMapper = GetRequiredService<IObjectMapper>();
            AsyncHelper.RunSync(async () => await InitializeAsync());
        }

        private async Task InitializeAsync()
        {
            var chainDto = await _chainAppService.CreateAsync(new ChainCreateDto
            {
                Id = DefaultChainId,
                Name = DebitTestData.DefaultNodeName,
                AElfChainId = DebitTestData.DefaultAElfChainId
            });
            DefaultChain = _objectMapper.Map<ChainDto, Chain>(chainDto);

            var projectToken = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChain.Id,
                Address = DebitTestData.ProjectTokenContractAddress,
                Symbol = DebitTestData.ProjectTokenSymbol,
                Decimals = DebitTestData.ProjectTokenDecimal
            });

            var usdtToken = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChain.Id,
                Address = DebitTestData.UsdtTokenContractAddress,
                Symbol = DebitTestData.UsdtTokenSymbol,
                Decimals = DebitTestData.UsdtTokenDecimal
            });

            await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChain.Id,
                Address = DebitTestData.CTokenAddress,
                Symbol = DebitTestData.CTokenSymbol,
                Decimals = DebitTestData.CTokenDecimal
            });
            
            CompController = await _compControllersRepository.InsertAsync(new CompController
            {
                ChainId = DefaultChain.Id,
                ControllerAddress = DebitTestData.ControllerAddress,
                CloseFactorMantissa = DebitTestData.CloseFactorMantissa,
                DividendTokenId = projectToken.Id
            }, true);
            
            CProjectToken = await _cTokensRepository.InsertAsync(new CToken
            {
                ChainId = DefaultChain.Id,
                UnderlyingTokenId = projectToken.Id,
                TotalCTokenMintAmount = DebitTestData.ZeroBalance,
                TotalUnderlyingAssetBorrowAmount = DebitTestData.ZeroBalance,
                TotalUnderlyingAssetReserveAmount = DebitTestData.ZeroBalance,
                TotalUnderlyingAssetAmount = DebitTestData.ZeroBalance,
                IsBorrowPaused = false,
                IsMintPaused = false,
                IsList = false,
                BorrowCompSpeed = DebitTestData.ZeroBalance,
                SupplyCompSpeed = DebitTestData.ZeroBalance,
                AccumulativeBorrowComp = DebitTestData.ZeroBalance,
                AccumulativeSupplyComp = DebitTestData.ZeroBalance,
                CollateralFactorMantissa = DebitTestData.CollateralFactorMantissa,
                ReserveFactorMantissa = DebitTestData.ReserveFactorMantissa,
                CompControllerId = CompController.Id,
                Address = DebitTestData.CTokenAddress,
                Symbol = DebitTestData.CTokenSymbol,
                Decimals = DebitTestData.CTokenDecimal
            }, true);

            await _esCompRepository.AddAsync(new Entities.Es.CompController(CompController.Id)
            {
                ChainId = CompController.ChainId,
                ControllerAddress = CompController.ControllerAddress,
                CloseFactorMantissa = CompController.CloseFactorMantissa,
                DividendToken = new Token
                {
                    Id = projectToken.Id,
                    ChainId = projectToken.ChainId,
                    Address = projectToken.Address,
                    Symbol = projectToken.Symbol,
                    Decimals = projectToken.Decimals
                },
            });

            await InsertTestCTokens();
        }

        private async Task InsertTestCTokens()
        {
            var underlyingToken = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChain.Id,
                Address = DebitTestData.UnderlyingTokenOneContractAddress,
                Symbol = DebitTestData.UnderlyingTokenOneSymbol,
                Decimals = DebitTestData.UnderlyingTokenOneDecimal
            });
            
            CTokenOne = await _cTokensRepository.InsertAsync(new CToken
            {
                ChainId = DefaultChain.Id,
                UnderlyingTokenId = underlyingToken.Id,
                TotalCTokenMintAmount = DebitTestData.ZeroBalance,
                TotalUnderlyingAssetBorrowAmount = DebitTestData.ZeroBalance,
                TotalUnderlyingAssetReserveAmount = DebitTestData.ZeroBalance,
                TotalUnderlyingAssetAmount = DebitTestData.ZeroBalance,
                IsBorrowPaused = false,
                IsMintPaused = false,
                IsList = false,
                BorrowCompSpeed = DebitTestData.ZeroBalance,
                SupplyCompSpeed = DebitTestData.ZeroBalance,
                AccumulativeBorrowComp = DebitTestData.ZeroBalance,
                AccumulativeSupplyComp = DebitTestData.ZeroBalance,
                CollateralFactorMantissa = DebitTestData.CollateralFactorMantissa,
                CompControllerId = CompController.Id,
                Address = DebitTestData.UnderlyingCTokenOneContractAddress,
                Symbol = DebitTestData.UnderlyingCTokenOneSymbol,
                Decimals = DebitTestData.UnderlyingCTokenOneDecimal,
                ReserveFactorMantissa = DebitTestData.ReserveFactorMantissa
            }, true);
        }
    }
}