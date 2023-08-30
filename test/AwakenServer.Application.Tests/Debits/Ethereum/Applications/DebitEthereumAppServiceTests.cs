using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Debits.DebitAppDto;
using AwakenServer.Debits.Entities.Es;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public partial class DebitEthereumAppServiceTests : AwakenServerDebitEthereumApplicationTestBase
    {
        private readonly IDebitAppService _debitAppService;
        private readonly INESTReaderRepository<CompController, Guid> _esCompRepository;
        private readonly INESTReaderRepository<CToken, Guid> _esCTokenRepository;
        private readonly INESTReaderRepository<CTokenUserInfo, Guid> _esUserInfoRepository;
        private readonly INESTReaderRepository<CTokenRecord, Guid> _esRecordRepository;

        public DebitEthereumAppServiceTests()
        {
            _debitAppService = GetRequiredService<IDebitAppService>();
            _esCompRepository = GetRequiredService<INESTReaderRepository<CompController, Guid>>();
            _esCTokenRepository = GetRequiredService<INESTReaderRepository<CToken, Guid>>();
            _esUserInfoRepository = GetRequiredService<INESTReaderRepository<CTokenUserInfo, Guid>>();
            _esRecordRepository = GetRequiredService<INESTReaderRepository<CTokenRecord, Guid>>();
        }

        [Fact(Skip = "no need")]
        public async Task GetCompControllerList_Should_Contain_Data()
        {
            var compControllers = await _debitAppService.GetCompControllerListAsync(new GetCompControllerInput());
            compControllers.Items.Count.ShouldBe(1);

            compControllers = await _debitAppService.GetCompControllerListAsync(new GetCompControllerInput
            {
                ChainId = DefaultChain.Id
            });
            compControllers.Items.Count.ShouldBe(1);

            compControllers = await _debitAppService.GetCompControllerListAsync(new GetCompControllerInput
            {
                CompControllerId = CompController.Id
            });
            compControllers.Items.Count.ShouldBe(1);
            var targetComp = compControllers.Items.First();
            targetComp.Id.ShouldNotBe(Guid.Empty);
            targetComp.ControllerAddress.ShouldBe(DebitTestData.ControllerAddress);
            targetComp.DividendToken.Id.ShouldNotBe(Guid.Empty);
            targetComp.CloseFactorMantissa.ShouldNotBe(null);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenList_Should_Contain_Data()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketListAsync(CTokenOne.Address, CompController.ControllerAddress);

            var cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput());
            cTokens.Items.Count.ShouldBe(2);

            cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
            {
                ChainId = DefaultChain.Id
            });
            cTokens.Items.Count.ShouldBe(2);

            cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
            {
                CompControllerId = CompController.Id
            });
            cTokens.Items.Count.ShouldBe(2);

            cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
            {
                CTokenId = CProjectToken.Id
            });
            cTokens.Items.Count.ShouldBe(1);

            cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
            {
                CTokenId = CTokenOne.Id
            });
            cTokens.Items.Count.ShouldBe(1);
            var cToken = cTokens.Items.First();
            cToken.Id.ShouldNotBe(Guid.Empty);
            cToken.UnderlyingToken.Id.ShouldNotBe(Guid.Empty);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenList_Should_Contain_Data_With_User()
        {
            var user1 = DebitTestData.Gui;
            var user2 = DebitTestData.Ming;
            await CreateRecords(user1, user2);
            var cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
            {
                CTokenId = CProjectToken.Id,
                User = user1
            });
            cTokens.Items.Count.ShouldBe(1);

            cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
            {
                CTokenId = CTokenOne.Id,
                User = user1
            });
            cTokens.Items.Count.ShouldBe(0);
        }

        // [Fact(Skip = "no need")] //todo
        // public async Task GetCTokenList_With_Apy_Should_Contain_Right_Data()
        // {
        //     var user1 = DebitTestData.Gui;
        //     var user2 = DebitTestData.Ming;
        //     await CreateRecords(user1, user2);
        //     var cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
        //     {
        //         CTokenId = CProjectToken.Id,
        //         IsWithApy = true
        //     });
        //     cTokens.Items.Count.ShouldBe(1);
        //     var cToken = cTokens.Items.First();
        //     cToken.BorrowApy.ShouldBeGreaterThan(0);
        //     cToken.SupplyApy.ShouldBeGreaterThan(0);
        //     cToken.BorrowInterest.ShouldBeGreaterThan(0);
        //     cToken.SupplyInterest.ShouldBeGreaterThan(0);
        //
        //     cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
        //     {
        //         CTokenId = CProjectToken.Id,
        //         IsWithUnderlyingTokenPrice = true,
        //         IsWithApy = true
        //     });
        //     cTokens.Items.Count.ShouldBe(1);
        //     cToken = cTokens.Items.First();
        //     cToken.BorrowApy.ShouldBeGreaterThan(0);
        //     cToken.SupplyApy.ShouldBeGreaterThan(0);
        //     cToken.BorrowInterest.ShouldBeGreaterThan(0);
        //     cToken.SupplyInterest.ShouldBeGreaterThan(0);
        // }

        [Fact(Skip = "no need")]
        public async Task GetCTokenList_With_Underlying_Token_Price_Should_Contain_Right_Data()
        {
            var user1 = DebitTestData.Gui;
            var user2 = DebitTestData.Ming;
            await CreateRecords(user1, user2);
            var cTokens = await _debitAppService.GetCTokenListAsync(new GetCTokenListInput
            {
                CTokenId = CProjectToken.Id,
                IsWithUnderlyingTokenPrice = true
            });
            cTokens.Items.Count.ShouldBe(1);
            var cToken = cTokens.Items.First();
            cToken.UnderlyingToken.TokenPrice.ShouldBeGreaterThan(0);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenUserInfoList_User_Should_Contain_Data()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketListAsync(CTokenOne.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            await MarketEnteredAsync(CTokenOne.Address, DebitTestData.Ming);

            var users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput());
            var user = users.Items.First();
            user.Id.ShouldNotBe(Guid.Empty);
            user.CompInfo.Id.ShouldNotBe(Guid.Empty);
            user.CompInfo.ChainId.ShouldNotBeNull();
            user.CompInfo.ControllerAddress.ShouldNotBe(null);
            user.CTokenInfo.Id.ShouldNotBe(Guid.Empty);
            user.CTokenInfo.Address.ShouldNotBe(null);
            user.UnderlyingToken.Id.ShouldNotBe(Guid.Empty);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenUserInfoList_Should_Contain_Data()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketListAsync(CTokenOne.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            await MarketEnteredAsync(CTokenOne.Address, DebitTestData.Ming);

            var users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput());
            users.Items.Count.ShouldBe(2);

            users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput
            {
                ChainId = DefaultChain.Id
            });
            users.Items.Count.ShouldBe(2);

            users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput
            {
                CompControllerId = CompController.Id
            });
            users.Items.Count.ShouldBe(2);

            users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput
            {
                CTokenId = CProjectToken.Id
            });
            users.Items.Count.ShouldBe(1);

            users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput
            {
                CTokenId = CTokenOne.Id
            });
            users.Items.Count.ShouldBe(1);

            users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput
            {
                User = DebitTestData.Gui
            });
            users.Items.Count.ShouldBe(1);

            users = await _debitAppService.GetCTokenUserInfoListAsync(new GetCTokenUserInfoInput
            {
                User = DebitTestData.Ming
            });
            users.Items.Count.ShouldBe(1);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenRecordList_Without_User_Should_Throw_Exception()
        {
            await Assert.ThrowsAsync<AbpValidationException>(async () =>
            {
                await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput());
            });
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenRecordList_Should_Contain_Data()
        {
            var user1 = DebitTestData.Gui;
            var user2 = DebitTestData.Ming;
            await CreateRecords(user1, user2);
            var records = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1
            });
            records.TotalCount.ShouldBe(2);

            records = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                ChainId = DefaultChain.Id
            });
            records.TotalCount.ShouldBe(2);

            records = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                CompControllerId = CompController.Id
            });
            records.TotalCount.ShouldBe(2);

            records = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                CTokenId = CProjectToken.Id
            });
            records.TotalCount.ShouldBe(2);

            records = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                CTokenId = CTokenOne.Id
            });
            records.TotalCount.ShouldBe(0);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenRecordList_With_Skip_Should_Contain_Valid_Records()
        {
            var user1 = DebitTestData.Gui;
            var user2 = DebitTestData.Ming;
            await CreateRecords(user1, user2);
            var recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                SkipCount = 1,
                Size = 1
            });
            recordListResultDto.TotalCount.ShouldBe(2);
            recordListResultDto.Items.Count.ShouldBe(1);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenRecordList_With_Order_Should_Contain_Valid_Records()
        {
            var user1 = DebitTestData.Gui;
            var user2 = DebitTestData.Ming;
            await CreateRecords(user1, user2);
            var recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                IsAscend = true
            });
            var records = recordListResultDto.Items;
            var orderedRecords = records.OrderBy(x => x.Timestamp).ToList();
            for (int i = 0; i < records.Count; i++)
            {
                Assert.Equal(records[i], orderedRecords[i]);
            }

            recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                IsAscend = false
            });
            records = recordListResultDto.Items;
            orderedRecords = records.OrderByDescending(x => x.Timestamp).ToList();
            for (int i = 0; i < records.Count; i++)
            {
                Assert.Equal(records[i], orderedRecords[i]);
            }
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenRecordList_With_StartTime_EndTime_Should_Contain_Valid_Records()
        {
            var user1 = DebitTestData.Gui;
            var user2 = DebitTestData.Ming;
            var currentDate = DateTime.Now;
            await CreateRecords(user1, user2);
            var recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                StartTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate) - 1000,
                EndTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate.AddMinutes(30))
            });
            recordListResultDto.TotalCount.ShouldBe(1);

            recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                StartTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate) - 1000,
                EndTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate.AddMinutes(140))
            });
            recordListResultDto.TotalCount.ShouldBe(2);

            recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                StartTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate.Subtract(TimeSpan.FromDays(1))),
                EndTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate.AddSeconds(-1))
            });
            recordListResultDto.TotalCount.ShouldBe(0);

            recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                StartTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate.AddHours(5)),
                EndTime = DateTimeHelper.ToUnixTimeMilliseconds(currentDate.AddHours(6))
            });
            recordListResultDto.TotalCount.ShouldBe(0);
        }

        [Fact(Skip = "no need")]
        public async Task GetCTokenRecordList_With_Behavior_Should_Contain_Valid_Records()
        {
            var user1 = DebitTestData.Gui;
            var user2 = DebitTestData.Ming;
            await CreateRecords(user1, user2);
            var recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                BehaviorType = BehaviorType.Mint
            });
            recordListResultDto.TotalCount.ShouldBe(1);

            recordListResultDto = await _debitAppService.GetCTokenRecordListAsync(new GetCTokenRecordInput
            {
                User = user1,
                BehaviorType = BehaviorType.Borrow
            });
            recordListResultDto.TotalCount.ShouldBe(1);

            var record = recordListResultDto.Items.First();
            record.Id.ShouldNotBe(Guid.Empty);
            record.Channel.ShouldBe("qm");
            record.Timestamp.ShouldNotBe(0);
            record.UnderlyingAssetToken.Id.ShouldNotBe(Guid.Empty);
            record.CToken.Id.ShouldNotBe(Guid.Empty);
            record.CompControllerInfo.Id.ShouldNotBe(Guid.Empty);
        }

        private async Task CreateRecords(string user1, string user2)
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketListAsync(CTokenOne.Address, CompController.ControllerAddress);

            await MarketEnteredAsync(CProjectToken.Address, user1);
            await MarketEnteredAsync(CTokenOne.Address, user2);

            var channel = "qm";
            var user1MintAmount = "500";
            var user1MintToken = "500";
            var user1MintTxHash = "user1Mint";
            var user1MintTimestamp = DateTime.Now;
            await MintAsync(CProjectToken.Address, user1, user1MintAmount, user1MintToken, channel, user1MintTxHash,
                user1MintTimestamp);

            var user2MintAmount = "500";
            var user2MintToken = "500";
            var user2MintTxHash = "user2Mint";
            var user2MintTimestamp = user1MintTimestamp.AddHours(1);
            await MintAsync(CTokenOne.Address, user2, user2MintAmount, user2MintToken, channel, user2MintTxHash,
                user2MintTimestamp);

            var user1BorrowAmount = "500";
            var user1BorrowTxHash = "user1Borrow";
            var user1BorrowTimestamp = user2MintTimestamp.AddHours(1);
            await BorrowAsync(CProjectToken.Address, user1, user1BorrowAmount, user1BorrowAmount, user1BorrowAmount,
                channel, user1BorrowTxHash,
                user1BorrowTimestamp);

            var user2BorrowAmount = "1500";
            var user2BorrowTxHash = "user2Borrow";
            var user2BorrowTimestamp = user1BorrowTimestamp.AddHours(1);
            await BorrowAsync(CTokenOne.Address, user2, user1BorrowAmount, user2BorrowAmount, user2BorrowAmount,
                channel, user2BorrowTxHash,
                user2BorrowTimestamp);
        }
    }
}