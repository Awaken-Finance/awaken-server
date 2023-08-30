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
            var borrow = borrowers.Single(x => x.User == borrower.ToBase58());
            borrow.TotalBorrowAmount.ShouldBe(DebitTestData.ZeroBalance);
        }

        private async Task LiquidateBorrowAsync(string cTokenVirtualAddress, Address liquidator, Address borrower,
            string repayAmount,
            string seizeTokens, string txHash, DateTime date)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var liquidateBorrowProcessor = GetRequiredService<IEventHandlerTestProcessor<LiquidateBorrow>>();
            await liquidateBorrowProcessor.HandleEventAsync(new LiquidateBorrow
            {
                Liquidator = liquidator,
                Borrower = borrower,
                RepayAmount = long.Parse(repayAmount),
                SeizeTokenAmount = long.Parse(seizeTokens),
                RepayAToken = Address.FromBase58(cTokenVirtualAddress),
                SeizeAToken = Address.FromBase58(cTokenVirtualAddress)
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr, txHash, timestamp));
        }
    }
}