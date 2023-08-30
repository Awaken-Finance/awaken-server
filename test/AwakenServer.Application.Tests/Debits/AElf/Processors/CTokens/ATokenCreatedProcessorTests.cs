using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.AToken;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.AElf.Tests
{
    public partial class DebitAElfAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task CreateAToken_Should_Insert_CTokens()
        {
            var cTokens = (await _esCTokenRepository.GetListAsync()).Item2;
            var targetTokenSymbol = DebitTestData.UsdtTokenSymbol;
            var targetCToken = cTokens.SingleOrDefault(x => x.UnderlyingToken.Symbol == targetTokenSymbol);
            targetCToken.ShouldBe(null);
            int targetTokenDecimal = 8;
            var targetCTokenAddress = DebitTestData.UsdtCTokenVirtualAddress;
            var tokenContract = DebitTestData.UsdtCTokenVirtualAddress;
            await CreateATokenAsync($"C{targetTokenSymbol}", $"C{targetTokenSymbol} - Name", targetTokenDecimal,
                targetCTokenAddress, targetTokenSymbol, tokenContract,
                Address.FromBase58(CompController.ControllerAddress));
            cTokens = (await _esCTokenRepository.GetListAsync()).Item2;
            targetCToken = cTokens.SingleOrDefault(x => x.UnderlyingToken.Symbol == targetTokenSymbol);
            targetCToken.ShouldNotBe(null);
        }
        
        private async Task CreateATokenAsync(string cTokenSymbol, string cTokenName, int cTokenDecimal,
            Address cTokenAddress, string underlyingTokenSymbol, Address tokenContract, Address controller, string txHash = null, DateTime date = new())
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var createATokenProcessor = GetRequiredService<IEventHandlerTestProcessor<TokenCreated>>();
            await createATokenProcessor.HandleEventAsync(new TokenCreated
            {
                Symbol = cTokenSymbol,
                TokenName = cTokenName,
                Decimals = cTokenDecimal,
                AToken = cTokenAddress,
                Underlying = underlyingTokenSymbol,
                TokenContract = tokenContract,
                Controller = controller

            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr, txHash, timestamp));
        }
    }
}