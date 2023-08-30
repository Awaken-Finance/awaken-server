using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public partial class DebitEthereumAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task Mint_Without_Confirmed_Should_Not_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetAmount.ShouldBe(DebitTestData.ZeroBalance);
            cToken.TotalCTokenMintAmount.ShouldBe(DebitTestData.ZeroBalance);

            await MintAsync(CProjectToken.Address, DebitTestData.Gui, "1000", "2000",
                confirmStatus: ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetAmount.ShouldBe(DebitTestData.ZeroBalance);
            cToken.TotalCTokenMintAmount.ShouldBe(DebitTestData.ZeroBalance);
            var (recordCount, _) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(0);
        }

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

        private async Task MintAsync(string cTokenAddress, string minter, string mintAmount, string mintTokens,
            string channel = null,
            string txHash = null, DateTime date = new(),
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var mintProcessor = GetRequiredService<IEventHandlerTestProcessor<Mint>>();
            await mintProcessor.HandleEventAsync(new Mint
            {
                Minter = minter,
                MintAmount = BigInteger.Parse(mintAmount),
                MintTokens = BigInteger.Parse(mintTokens),
                Channel = channel
            }, GetDefaultEventContext(cTokenAddress, txHash, timestamp, confirmStatus));
        }
    }
}