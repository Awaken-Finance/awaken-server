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
        public async Task Redeem_With_Confirmed_Should_Modify_CToken_And_Add_Records()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);

            var mintAmount = "1000";
            var mintCToken = "2000";
            var minter = DebitTestData.Ming;
            await MintAsync(CProjectToken.Address, minter, mintAmount, mintCToken);
            
            var redeemTxHash = "redeem1";
            var redeemTimestamp = DateTime.Now;
            var redeemCTokens = mintCToken;
            await RedeemAsync(CProjectToken.Address, minter, mintAmount, redeemCTokens, redeemTxHash, redeemTimestamp);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.TotalCTokenMintAmount.ShouldBe(DebitTestData.ZeroBalance);
            cToken.TotalUnderlyingAssetAmount.ShouldBe(DebitTestData.ZeroBalance);
            
            var (recordCount, records) = await _esRecordRepository.GetListAsync();
            recordCount.ShouldBe(2);
            var targetRecord = records.First(x => x.BehaviorType == BehaviorType.Redeem);
            targetRecord.CTokenAmount.ShouldBe(mintCToken);
            targetRecord.UnderlyingTokenAmount.ShouldBe(mintAmount);
        }
        
        
        private async Task RedeemAsync(string cTokenVirtualAddress, Address redeemer, string redeemAmount, string redeemTokens,
            string txHash, DateTime date)
        {
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(date);
            var redeemProcessor = GetRequiredService<IEventHandlerTestProcessor<Redeem>>();
            await redeemProcessor.HandleEventAsync(new Redeem
            {
                AToken = Address.FromBase58(cTokenVirtualAddress),
                Sender = redeemer,
                ATokenAmount = long.Parse(redeemTokens),
                UnderlyingAmount = long.Parse(redeemAmount)
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr, txHash, timestamp));
        }
    }
}