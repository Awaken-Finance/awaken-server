using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using Volo.Abp.Threading;

namespace AwakenServer.Price
{
    public class MockPriceTestBase : AwakenServerTestBase<MockPriceTestModule>
    {

        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly ChainTestHelper _chainTestHelper;
        // protected readonly ILendingTokenPriceAppService LendingTokenPriceAppService;
        // protected readonly IOtherLpTokenAppService OtherLpTokenAppService;
        protected string ChainId;
        protected Guid TokenBtcId;
        protected Tokens.Token TokenBtc;
        protected Guid TokenEthId;
        protected Tokens.Token TokenEth;
        protected Guid TokenSashimiId;
        protected Tokens.Token TokenSashimi;
        protected Guid TokenUSDTId;
        protected Tokens.Token TokenUSDT;
        protected long Timestamp = 1636784478000;

        protected MockPriceTestBase()
        {
            // LendingTokenPriceAppService = GetRequiredService<ILendingTokenPriceAppService>();
            // OtherLpTokenAppService = GetRequiredService<IOtherLpTokenAppService>();
            _chainAppService = GetRequiredService<IChainAppService>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
            _chainTestHelper = GetRequiredService<ChainTestHelper>();
            
            var chainDto = AsyncHelper.RunSync(async () => await _chainTestHelper.CreateAsync(new ChainCreateDto
            {
                Name = "Ethereum"
            }));
            ChainId = chainDto.Id;

            var tokenDto = AsyncHelper.RunSync(async () => await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                Address = "0xBTC",
                Decimals = 18,
                Symbol = "BTC",
                ChainId = ChainId
            }));
            TokenBtcId = tokenDto.Id;
            TokenBtc = new Tokens.Token
            {
                Id = tokenDto.Id,
                Address = tokenDto.Address,
                Decimals = tokenDto.Decimals,
                Symbol = tokenDto.Symbol,
                ChainId = chainDto.Id
            };
            
            tokenDto = AsyncHelper.RunSync(async () => await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                Address = "0xETH",
                Decimals = 18,
                Symbol = "ETH",
                ChainId = ChainId
            }));
            TokenEthId = tokenDto.Id;
            TokenEth = new Tokens.Token
            {
                Id = tokenDto.Id,
                Address = tokenDto.Address,
                Decimals = tokenDto.Decimals,
                Symbol = tokenDto.Symbol,
                ChainId = chainDto.Id
            };
            
            tokenDto = AsyncHelper.RunSync(async () => await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                Address = "0xSASHIMI",
                Decimals = 18,
                Symbol = "SASHIMI",
                ChainId = ChainId
            }));
            TokenSashimiId = tokenDto.Id;
            TokenSashimi = new Tokens.Token
            {
                Id = tokenDto.Id,
                Address = tokenDto.Address,
                Decimals = tokenDto.Decimals,
                Symbol = tokenDto.Symbol,
                ChainId = chainDto.Id
            };
            
            tokenDto = AsyncHelper.RunSync(async () => await _tokenAppService.CreateAsync(new TokenCreateDto
            {
                Address = "0xUSDT",
                Decimals = 18,
                Symbol = "USDT",
                ChainId = ChainId
            }));
            TokenUSDTId = tokenDto.Id;
            TokenUSDT = new Tokens.Token
            {
                Id = tokenDto.Id,
                Address = tokenDto.Address,
                Decimals = tokenDto.Decimals,
                Symbol = tokenDto.Symbol,
                ChainId = chainDto.Id
            };
        }
        
        protected async Task<List<LendingTokenPriceCreateOrUpdateDto>> CreateLendingTokenPrices()
        {
            var btcPrice = new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = ChainId,
                TokenId = TokenBtcId,
                Price = "63678.72",
                PriceValue = 63678.72,
                Timestamp = Timestamp,
                BlockNumber = 100
            };
            // await LendingTokenPriceAppService.CreateOrUpdateAsync(btcPrice);
            
            var ethPrice = new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = ChainId,
                TokenId = TokenEthId,
                Price = "4611.03",
                PriceValue = 4611.03,
                Timestamp = Timestamp,
                BlockNumber = 100
            };
            // await LendingTokenPriceAppService.CreateOrUpdateAsync(ethPrice);
            return new List<LendingTokenPriceCreateOrUpdateDto> {btcPrice, ethPrice};
        }
        
        protected async Task<OtherLpTokenCreateDto> CreateOtherLpTokenAsync()
        {
            var createDto = new OtherLpTokenCreateDto
            {
                Address = "0xOtherLPToken",
                Reserve0 = "200",
                Reserve1 = "100",
                ChainId = ChainId,
                Reserve0Value = 200,
                Reserve1Value = 100,
                Token0Id = TokenBtcId,
                Token1Id = TokenEthId
            };
            // await OtherLpTokenAppService.CreateAsync(createDto);
            return createDto;
        }
    }
}