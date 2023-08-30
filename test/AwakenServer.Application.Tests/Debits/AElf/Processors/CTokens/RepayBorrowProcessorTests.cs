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
        public async Task RepayBorrow_With_Confirmed_Should_Modify_CToken_And_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var repayAmount = "100";
            var accountBorrows = "50";
            var totalBorrows = "10000";
            var txHash = "repayborrow1";
            var timestamp = DateTime.Now;
            var payer = DebitTestData.Xi;
            var borrower = DebitTestData.Gui;

            await RepayBorrowAsync(CProjectToken.Address, payer, borrower, repayAmount, accountBorrows, totalBorrows,
                txHash,
                timestamp);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetBorrowAmount.ShouldBe(totalBorrows);
            var (recordCount, records) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(2);
            var targetRecord = records.First();
            targetRecord.UnderlyingTokenAmount.ShouldBe(repayAmount);

            var borrowers = (await _esUserInfoRepository.GetListAsync()).Item2;
            var borrow = borrowers.Single(x => x.User == borrower.ToBase58());
            borrow.TotalBorrowAmount.ShouldBe(accountBorrows);
        }

        private async Task RepayBorrowAsync(string cTokenVirtualAddress, Address payer, Address borrower, string repayAmount,
            string accountBorrows, string totalBorrows, string txHash, DateTime date)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var redeemProcessor = GetRequiredService<IEventHandlerTestProcessor<RepayBorrow>>();
            await redeemProcessor.HandleEventAsync(new RepayBorrow
            {
                AToken = Address.FromBase58(cTokenVirtualAddress),
                Payer = payer,
                Borrower = borrower,
                BorrowBalance = long.Parse(accountBorrows),
                Amount = long.Parse(repayAmount),
                TotalBorrows = long.Parse(totalBorrows)
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr, txHash, timestamp));
        }
    }
}