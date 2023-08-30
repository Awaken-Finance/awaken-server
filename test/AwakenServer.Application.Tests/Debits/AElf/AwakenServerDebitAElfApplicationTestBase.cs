using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Debits.Entities.Ef;
using AwakenServer.Tokens;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;
using EsCompController = AwakenServer.Debits.Entities.Es.CompController;

namespace AwakenServer.Debits.AElf
{
    public class AwakenServerDebitAElfApplicationTestBase : AwakenServerTestBase<AwakenServerDebitTestModule>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly IRepository<CompController> _compControllersRepository;
        private readonly IRepository<CToken> _cTokensRepository;
        private readonly IObjectMapper _objectMapper;
        protected Chain DefaultChain;
        protected CompController CompController;
        protected CToken CProjectToken;
        protected CToken CTokenOne;
        protected static string DefaultChainId = Guid.NewGuid().ToString();

            public AwakenServerDebitAElfApplicationTestBase()
        {
            _chainAppService = GetRequiredService<IChainAppService>();
            _compControllersRepository = GetRequiredService<Volo.Abp.Domain.Repositories.IRepository<CompController>>();
            _cTokensRepository = GetRequiredService<Volo.Abp.Domain.Repositories.IRepository<CToken>>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
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
                Address = DebitTestData.ProjectTokenContractAddress.ToBase58(),
                Symbol = DebitTestData.ProjectTokenSymbol,
                Decimals = DebitTestData.ProjectTokenDecimal
            });

            var usdtToken = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChain.Id,
                Address = DebitTestData.UsdtTokenContractAddress.ToBase58(),
                Symbol = DebitTestData.UsdtTokenSymbol,
                Decimals = DebitTestData.UsdtTokenDecimal
            });

            await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChain.Id,
                Address = DebitTestData.CTokenVirtualAddress.ToBase58(),
                Symbol = DebitTestData.CTokenSymbol,
                Decimals = DebitTestData.CTokenDecimal
            });
            
            CompController = await _compControllersRepository.InsertAsync(new CompController
            {
                ChainId = DefaultChain.Id,
                ControllerAddress = DebitTestData.ControllerAddress.ToBase58(),
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
                Address = DebitTestData.CTokenVirtualAddress.ToBase58(),
                Symbol = DebitTestData.CTokenSymbol,
                Decimals = DebitTestData.CTokenDecimal
            }, true);
            await InsertTestCTokens();
        }

        private async Task InsertTestCTokens()
        {
            var underlyingToken = await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                ChainId = DefaultChain.Id,
                Address = DebitTestData.UnderlyingTokenOneContractAddress.ToBase58(),
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
                Address = DebitTestData.UnderlyingCTokenOneContractAddress.ToBase58(),
                Symbol = DebitTestData.UnderlyingCTokenOneSymbol,
                Decimals = DebitTestData.UnderlyingCTokenOneDecimal,
                ReserveFactorMantissa = DebitTestData.ReserveFactorMantissa
            }, true);
        }
    }
}