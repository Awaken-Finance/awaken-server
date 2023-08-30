using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Dtos;
using Shouldly;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class OtherLpTokenAppServiceTests : PriceTestBase
    {
        private readonly IOtherLpTokenAppService _otherLpTokenAppService;
        private readonly IOtherLpTokenRepository _otherLpTokenRepository;
        private readonly INESTRepository<Index.OtherLpToken, Guid> _otherLpTokenIndexRepositry;

        public OtherLpTokenAppServiceTests()
        {
            _otherLpTokenAppService = GetRequiredService<IOtherLpTokenAppService>();
            _otherLpTokenRepository = GetRequiredService<IOtherLpTokenRepository>();
            _otherLpTokenIndexRepositry = GetRequiredService<INESTRepository<Index.OtherLpToken, Guid>>();
        }

        [Fact(Skip = "no need")]
        public async Task CreateTest()
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
            await _otherLpTokenAppService.CreateAsync(createDto);

            var otherLpToken = await _otherLpTokenRepository.GetAsync(o => o.ChainId == ChainId && o.Address == createDto.Address);
            otherLpToken.Address.ShouldBe(createDto.Address);
            otherLpToken.Reserve0.ShouldBe(createDto.Reserve0);
            otherLpToken.Reserve1.ShouldBe(createDto.Reserve1);
            otherLpToken.ChainId.ShouldBe(createDto.ChainId);
            otherLpToken.Reserve0Value.ShouldBe(createDto.Reserve0Value);
            otherLpToken.Reserve1Value.ShouldBe(createDto.Reserve1Value);
            otherLpToken.Token0Id.ShouldBe(createDto.Token0Id);
            otherLpToken.Token1Id.ShouldBe(createDto.Token1Id);
            
            var otherLpTokenIndex = await _otherLpTokenIndexRepositry.GetAsync(otherLpToken.Id);
            
            otherLpTokenIndex.Address.ShouldBe(createDto.Address);
            otherLpTokenIndex.Reserve0.ShouldBe(createDto.Reserve0);
            otherLpTokenIndex.Reserve1.ShouldBe(createDto.Reserve1);
            otherLpTokenIndex.ChainId.ShouldBe(createDto.ChainId);
            otherLpTokenIndex.Reserve0Value.ShouldBe(createDto.Reserve0Value);
            otherLpTokenIndex.Reserve1Value.ShouldBe(createDto.Reserve1Value);
            otherLpTokenIndex.Token0.ShouldBeEquivalentTo(TokenBtc);
            otherLpTokenIndex.Token1.ShouldBeEquivalentTo(TokenEth);
        }
        
        [Fact(Skip = "no need")]
        public async Task UpdateTest()
        {
            var createDto = await CreateOtherLpTokenAsync();
            
            var otherLpToken = await _otherLpTokenRepository.GetAsync(o => o.ChainId == ChainId && o.Address == createDto.Address);
            
            var otherLpTokenDto = new OtherLpTokenDto
            {
                Id = otherLpToken.Id,
                Address = "0xOtherLPToken",
                Reserve0 = "200",
                Reserve1 = "100",
                ChainId = ChainId,
                Reserve0Value = 300,
                Reserve1Value = 200,
                Token0Id = TokenBtcId,
                Token1Id = TokenEthId
            };
            await _otherLpTokenAppService.UpdateAsync(otherLpTokenDto);

            otherLpToken = await _otherLpTokenRepository.GetAsync(otherLpToken.Id);

            otherLpToken.Address.ShouldBe(otherLpTokenDto.Address);
            otherLpToken.Reserve0.ShouldBe(otherLpTokenDto.Reserve0);
            otherLpToken.Reserve1.ShouldBe(otherLpTokenDto.Reserve1);
            otherLpToken.ChainId.ShouldBe(otherLpTokenDto.ChainId);
            otherLpToken.Reserve0Value.ShouldBe(otherLpTokenDto.Reserve0Value);
            otherLpToken.Reserve1Value.ShouldBe(otherLpTokenDto.Reserve1Value);
            otherLpToken.Token0Id.ShouldBe(otherLpTokenDto.Token0Id);
            otherLpToken.Token1Id.ShouldBe(otherLpTokenDto.Token1Id);
            
            var otherLpTokenIndex = await _otherLpTokenIndexRepositry.GetAsync(otherLpToken.Id);
            
            otherLpTokenIndex.Address.ShouldBe(otherLpTokenDto.Address);
            otherLpTokenIndex.Reserve0.ShouldBe(otherLpTokenDto.Reserve0);
            otherLpTokenIndex.Reserve1.ShouldBe(otherLpTokenDto.Reserve1);
            otherLpTokenIndex.ChainId.ShouldBe(otherLpTokenDto.ChainId);
            otherLpTokenIndex.Reserve0Value.ShouldBe(otherLpTokenDto.Reserve0Value);
            otherLpTokenIndex.Reserve1Value.ShouldBe(otherLpTokenDto.Reserve1Value);
            otherLpTokenIndex.Token0.ShouldBeEquivalentTo(TokenBtc);
            otherLpTokenIndex.Token1.ShouldBeEquivalentTo(TokenEth);
        }

        [Fact(Skip = "no need")]
        public async Task GetByAddressTest()
        {
            // Get empty
            var otherLpTokenDto = await _otherLpTokenAppService.GetByAddressAsync(ChainId, "");
            otherLpTokenDto.ShouldBeNull();
            
            //Get otherLpToken
            var createDto = await CreateOtherLpTokenAsync();
            otherLpTokenDto = await _otherLpTokenAppService.GetByAddressAsync(ChainId, createDto.Address);
            
            otherLpTokenDto.Address.ShouldBe(createDto.Address);
            otherLpTokenDto.Reserve0.ShouldBe(createDto.Reserve0);
            otherLpTokenDto.Reserve1.ShouldBe(createDto.Reserve1);
            otherLpTokenDto.ChainId.ShouldBe(createDto.ChainId);
            otherLpTokenDto.Reserve0Value.ShouldBe(createDto.Reserve0Value);
            otherLpTokenDto.Reserve1Value.ShouldBe(createDto.Reserve1Value);
            otherLpTokenDto.Token0Id.ShouldBe(createDto.Token0Id);
            otherLpTokenDto.Token1Id.ShouldBe(createDto.Token1Id);
        }

        [Fact(Skip = "no need")]
        public async Task GetOtherLpTokenIndexListTest()
        {
            //Get empty list
            var list = await _otherLpTokenAppService.GetOtherLpTokenIndexListAsync(ChainId, new[] {"0xOtherLPToken"});
            list.Count.ShouldBe(0);

            // Get list
            var createDto = await CreateOtherLpTokenAsync();
            
            list = await _otherLpTokenAppService.GetOtherLpTokenIndexListAsync(ChainId, new[] {"0xOtherLPToken"});
            list.Count.ShouldBe(1);
            list[0].Address.ShouldBe(createDto.Address);
            list[0].Reserve0.ShouldBe(createDto.Reserve0);
            list[0].Reserve1.ShouldBe(createDto.Reserve1);
            list[0].ChainId.ShouldBe(createDto.ChainId);
            list[0].Reserve0Value.ShouldBe(createDto.Reserve0Value);
            list[0].Reserve1Value.ShouldBe(createDto.Reserve1Value);
            list[0].Token0.Id.ShouldBeEquivalentTo(TokenBtc.Id);
            list[0].Token1.Id.ShouldBeEquivalentTo(TokenEth.Id);
        }
    }
}