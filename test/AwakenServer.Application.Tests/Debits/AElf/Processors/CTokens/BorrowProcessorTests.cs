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
        public async Task Borrow_With_Confirmed_Should_Modify_CToken_And_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);

            var borrowAmount = "100";
            var accountBorrow = "101";
            var totalBorrows = "1000";
            var txHash = "borrow1";
            var channel = "hb";
            var timestamp = DateTime.Now;
            var borrower = DebitTestData.Gui;
            await BorrowAsync(CProjectToken.Address, borrower,
                borrowAmount, accountBorrow,
                totalBorrows, channel, txHash,
                timestamp);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.TotalUnderlyingAssetBorrowAmount.ShouldBe(totalBorrows);
            var (recordCount, records) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(1);
            var targetRecord = records.First();
            targetRecord.Id.ShouldNotBe(Guid.Empty);
            targetRecord.CompControllerInfo.Id.ShouldNotBe(Guid.Empty);
            targetRecord.UnderlyingAssetToken.Id.ShouldNotBe(Guid.Empty);
            targetRecord.CToken.Id.ShouldNotBe(Guid.Empty);
            targetRecord.UnderlyingTokenAmount.ShouldBe(borrowAmount);
            //targetRecord.Channel.ShouldBe(channel); todo

            var borrowers = (await _esUserInfoRepository.GetListAsync()).Item2;
            var borrow = borrowers.Single(x => x.User == borrower.ToBase58());
            borrow.TotalBorrowAmount.ShouldBe(accountBorrow);
        }

        private async Task BorrowAsync(string cTokenVirtualAddress, Address borrower, string borrowAmount,
            string accountBorrows, string totalBorrows, string channel, string txHash, DateTime date)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var borrowProcessor = GetRequiredService<IEventHandlerTestProcessor<Borrow>>();
            await borrowProcessor.HandleEventAsync(new Borrow
            {
                Borrower = borrower,
                AToken = Address.FromBase58(cTokenVirtualAddress),
                Amount = long.Parse(borrowAmount),
                BorrowBalance = long.Parse(accountBorrows),
                TotalBorrows = long.Parse(totalBorrows),
                // Channel = channel todo add channel
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr, txHash, timestamp));
        }
    }
}