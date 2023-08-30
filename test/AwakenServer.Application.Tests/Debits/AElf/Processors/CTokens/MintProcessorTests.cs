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
        public async Task Mint_With_Confirmed_Should_Modify_CToken_And_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetAmount.ShouldBe(DebitTestData.ZeroBalance);
            cToken.TotalCTokenMintAmount.ShouldBe(DebitTestData.ZeroBalance);

            var mintAmount = "1000";
            var mintCToken = "2000";
            var channel = "me";
            await MintAsync(CProjectToken.Address, DebitTestData.Gui, mintAmount, mintCToken, channel);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetAmount.ShouldBe(mintAmount);
            cToken.TotalCTokenMintAmount.ShouldBe(mintCToken);
            var (recordCount, records) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(1);
            var targetRecord = records.First();
            targetRecord.Id.ShouldNotBe(Guid.Empty);
            targetRecord.CompControllerInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.UnderlyingAssetToken.Id.ShouldNotBe(Guid.Empty);
            targetRecord.CToken.Id.ShouldNotBe(Guid.Empty);
            targetRecord.CTokenAmount.ShouldBe(mintCToken);
            targetRecord.UnderlyingTokenAmount.ShouldBe(mintAmount);
            targetRecord.Channel.ShouldBe(channel);
        }

        private async Task MintAsync(string cTokenVirtualAddress, Address minter, string mintAmount, string mintTokens,
            string channel = "default",
            string txHash = null, DateTime date = new())
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var mintProcessor = GetRequiredService<IEventHandlerTestProcessor<Mint>>();
            await mintProcessor.HandleEventAsync(new Mint
            {
                AToken = Address.FromBase58(cTokenVirtualAddress),
                Sender = minter,
                ATokenAmount = long.Parse(mintTokens),
                UnderlyingAmount = long.Parse(mintAmount),
                Channel = channel
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr, txHash, timestamp));
        }
    }
}