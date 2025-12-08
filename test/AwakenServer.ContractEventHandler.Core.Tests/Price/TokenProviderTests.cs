using System;
using System.Threading.Tasks;
using AwakenServer.ContractEventHandler;
using Shouldly;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class TokenProviderTests : PriceProcessorTestBase
    {
        private readonly ITokenProvider _tokenProvider;

        public TokenProviderTests()
        {
            _tokenProvider = GetRequiredService<ITokenProvider>();
        }

        [Fact(Skip = "no need")]
        public async Task Test()
        {
            const string eth = "0xETH";
            var tokenDto = await _tokenProvider.GetOrAddTokenAsync(ChainId, "Ethereum", eth);
            tokenDto.Address.ShouldBe(eth);
            tokenDto.Symbol.ShouldBe("ETH");
            tokenDto.Decimals.ShouldBe(18);
            tokenDto.Id.ShouldNotBe(Guid.Empty);
            
            var newTokenDto = await _tokenProvider.GetOrAddTokenAsync(ChainId, "Ethereum", eth);
            newTokenDto.Address.ShouldBe(eth);
            newTokenDto.Symbol.ShouldBe("ETH");
            newTokenDto.Decimals.ShouldBe(18);
            newTokenDto.Id.ShouldBe(tokenDto.Id);
        }
    }
}