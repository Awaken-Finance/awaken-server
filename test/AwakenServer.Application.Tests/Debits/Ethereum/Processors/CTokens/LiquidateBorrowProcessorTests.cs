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
        public async Task LiquidateBorrow_Without_Confirmed_Should_Not_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var repayAmount = "100";
            var seizeTokens = "0";
            var txHash = "borrow1";
            var timestamp = DateTime.Now;
            var liquidator = DebitTestData.Xi;
            var borrower = DebitTestData.Gui;

            await LiquidateBorrowAsync(CProjectToken.Address, liquidator, borrower, repayAmount, seizeTokens, txHash,
                timestamp, ContractEventStatus.Unconfirmed);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetBorrowAmount.ShouldBe(DebitTestData.ZeroBalance);
            var (recordCount, _) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(0);
        }

        [Fact(Skip = "no need")]
        public async Task LiquidateBorrow_With_Confirmed_Should_Modify_CToken_And_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);

            var mintAmount = "1000";
            var mintCToken = "2000";
            await MintAsync(CProjectToken.Address, DebitTestData.Ming, mintAmount, mintCToken);

            var borrowAmount = "100";
            var accountBorrow = "100";
            var totalBorrows = "100";
            var borrowTxHash = "borrow1";
            var channel = "xc";
            var borrowTimestamp = DateTime.Now;
            await BorrowAsync(CProjectToken.Address, DebitTestData.Gui, borrowAmount, accountBorrow, totalBorrows, channel,
                borrowTxHash,
                borrowTimestamp);

            var repayAmount = borrowAmount;
            var seizeTokens = "50";
            var txHash = "liquidate1";
            var timestamp = borrowTimestamp.AddHours(1);
            var liquidator = DebitTestData.Xi;
            var borrower = DebitTestData.Gui;
            await LiquidateBorrowAsync(CProjectToken.Address, liquidator, borrower, repayAmount, seizeTokens, txHash,
                timestamp);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetBorrowAmount.ShouldBe(DebitTestData.ZeroBalance);
            var (recordCount, _) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(4);

            var borrowers = (await _esUserInfoRepository.GetListAsync()).Item2;
            var borrow = borrowers.Single(x => x.User == borrower);
            borrow.TotalBorrowAmount.ShouldBe(DebitTestData.ZeroBalance);
        }

        private async Task LiquidateBorrowAsync(string cTokenAddress, string liquidator, string borrower,
            string repayAmount,
            string seizeTokens, string txHash, DateTime date,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var liquidateBorrowProcessor = GetRequiredService<IEventHandlerTestProcessor<LiquidateBorrow>>();
            await liquidateBorrowProcessor.HandleEventAsync(new LiquidateBorrow
            {
                Liquidator = liquidator,
                Borrower = borrower,
                RepayAmount = BigInteger.Parse(repayAmount),
                SeizeTokens = BigInteger.Parse(seizeTokens),
                CTokenCollateral = cTokenAddress
            }, GetDefaultEventContext(cTokenAddress, txHash, timestamp, confirmStatus));
        }
    }
}