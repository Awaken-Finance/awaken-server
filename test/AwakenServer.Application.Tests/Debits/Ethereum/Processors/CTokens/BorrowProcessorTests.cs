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
        public async Task Borrow_Without_Confirmed_Should_Not_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var borrowAmount = "100";
            var accountBorrow = "0";
            var totalBorrows = "1000";
            var txHash = "borrow1";
            var channel = "ba";
            var timestamp = DateTime.Now;
            await BorrowAsync(CProjectToken.Address, DebitTestData.Gui, borrowAmount, accountBorrow, totalBorrows, channel,
                txHash,
                timestamp, ContractEventStatus.Unconfirmed);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetBorrowAmount.ShouldBe(DebitTestData.ZeroBalance);
            var (recordCount, _) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(0);
        }

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
            await BorrowAsync(CProjectToken.Address, borrower, borrowAmount, accountBorrow, totalBorrows, channel, txHash,
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
            targetRecord.Channel.ShouldBe(channel);

            var borrowers = (await _esUserInfoRepository.GetListAsync()).Item2;
            var borrow = borrowers.Single(x => x.User == borrower);
            borrow.TotalBorrowAmount.ShouldBe(accountBorrow);
        }

        private async Task BorrowAsync(string cTokenAddress, string borrower, string borrowAmount,
            string accountBorrows, string totalBorrows, string channel, string txHash, DateTime date,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var borrowProcessor = GetRequiredService<IEventHandlerTestProcessor<Borrow>>();
            await borrowProcessor.HandleEventAsync(new Borrow
            {
                Borrower = borrower,
                BorrowAmount = BigInteger.Parse(borrowAmount),
                AccountBorrows = BigInteger.Parse(accountBorrows),
                TotalBorrows = BigInteger.Parse(totalBorrows),
                Channel = channel
            }, GetDefaultEventContext(cTokenAddress, txHash, timestamp, confirmStatus));
        }
    }
}