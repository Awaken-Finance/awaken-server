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
        public async Task RepayBorrow_Without_Confirmed_Should_Not_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var repayAmount = "100";
            var accountBorrows = "0";
            var totalBorrows = "10000";
            var txHash = "repayborrow1";
            var timestamp = DateTime.Now;
            var payer = DebitTestData.Xi;
            var borrower = DebitTestData.Gui;

            await RepayBorrowAsync(CProjectToken.Address, payer, borrower, repayAmount, accountBorrows, totalBorrows,
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
            var borrow = borrowers.Single(x => x.User == borrower);
            borrow.TotalBorrowAmount.ShouldBe(accountBorrows);
        }

        private async Task RepayBorrowAsync(string cTokenAddress, string payer, string borrower, string repayAmount,
            string accountBorrows, string totalBorrows, string txHash, DateTime date,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date) / 1000;
            var redeemProcessor = GetRequiredService<IEventHandlerTestProcessor<RepayBorrow>>();
            await redeemProcessor.HandleEventAsync(new RepayBorrow
            {
                Payer = payer,
                Borrower = borrower,
                AccountBorrows = BigInteger.Parse(accountBorrows),
                RepayAmount = BigInteger.Parse(repayAmount),
                TotalBorrows = BigInteger.Parse(totalBorrows)
            }, GetDefaultEventContext(cTokenAddress, txHash, timestamp, confirmStatus));
        }
    }
}