using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Provider;
using AwakenServer.Trade.Dtos;
using Microsoft.AspNetCore.Routing.Matching;
using Nest;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Trade
{
    public class TradeRecordAppServiceTests : TradeTestBase
    {
        private readonly ITradeRecordAppService _tradeRecordAppService;
        private readonly INESTRepository<Index.TradePair, Guid> _tradePairIndexRepository;
        private readonly INESTRepository<Index.TradeRecord, Guid> _tradeRecordIndexRepository;
        private readonly ILocalEventBus _eventBus;
        private readonly MockGraphQLProvider _graphQlProvider;

        public TradeRecordAppServiceTests()
        {
            _tradeRecordAppService = GetRequiredService<ITradeRecordAppService>();
            _tradePairIndexRepository = GetRequiredService<INESTRepository<Index.TradePair, Guid>>();
            _tradeRecordIndexRepository = GetRequiredService<INESTRepository<Index.TradeRecord, Guid>>();
            _eventBus = GetRequiredService<ILocalEventBus>();
            _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        }

        [Fact]
        public async Task SwapTest()
        {
            var swapRecordDto = new SwapRecordDto
            {
                ChainId = "tDVV",
                PairAddress = "2Ck7Hg4LD3LMHiKpbbPJuyVXv1zbFLzG7tP6ZmWf3L2ajwtSnk",
                Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
                TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = 100,
                AmountIn = 1,
                SymbolOut = "USDT",
                SymbolIn = "ELF",
                Channel = "test",
                BlockHeight = 99
            };
            var tradePair = new Index.TradePair()
            {
                Id = TradePairEthUsdtId,
                ChainId = "tDVV",
                Address = "2Ck7Hg4LD3LMHiKpbbPJuyVXv1zbFLzG7tP6ZmWf3L2ajwtSnk",
                Token0 = new Token()
                {
                    Symbol = "ELF",
                    Decimals = 8
                },
                Token1 = new Token()
                {
                    Symbol = "USDT",
                    Decimals = 8
                },
            };
            await _tradePairIndexRepository.AddAsync(tradePair);
            await _tradeRecordAppService.CreateAsync(swapRecordDto);
            _graphQlProvider.AddSwapRecord(swapRecordDto);
            var swapList = _graphQlProvider.GetSwapRecordsAsync(ChainId, 0 , 100);
            swapList.Result.Count.ShouldBe(1);
            var ret = await _tradeRecordAppService.CreateAsync(swapRecordDto);
            ret.ShouldBe(false);
            await _tradePairIndexRepository.DeleteAsync(tradePair.Id);
            swapRecordDto.TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d36";
            ret = await _tradeRecordAppService.CreateAsync(swapRecordDto);
            ret.ShouldBe(false);
        }
        
        [Fact]
        public async Task RevertTest()
        {
            var id = Guid.NewGuid();
            var chainId = "tDVV";
            var blockHeight = 1L;
            var transactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37";
            var address = "2Ck7Hg4LD3LMHiKpbbPJuyVXv1zbFLzG7tP6ZmWf3L2ajwtSnk";
            var tradePairId = Guid.NewGuid();
            var dto = new SwapRecordDto()
            {
                ChainId = chainId,
                TransactionHash = transactionHash,
                Sender = address,
                BlockHeight = blockHeight,
            };
            await _tradeRecordAppService.CreateCacheAsync(tradePairId, dto);
            await _tradeRecordAppService.CreateCacheAsync(tradePairId, dto);
            dto.TransactionHash = "AAA";
            await _tradeRecordAppService.CreateCacheAsync(tradePairId, dto);
            await _tradeRecordIndexRepository.AddAsync(new Index.TradeRecord()
            {
                Id = id,
                ChainId = chainId,
                TransactionHash = transactionHash,
                Address = address,
                BlockHeight = blockHeight
            });
            await _tradeRecordIndexRepository.AddAsync(new Index.TradeRecord()
            {
                Id = Guid.NewGuid(),
                ChainId = chainId,
                TransactionHash = "AAA",
                Address = address,
                BlockHeight = blockHeight
            });
            _graphQlProvider.AddSwapRecord(dto);
            await _tradeRecordAppService.RevertAsync(chainId);
            await _tradeRecordAppService.RevertAsync(chainId);

            for (var i = 2; i < 104; i++)
            {
                dto.BlockHeight = i;
                await _tradeRecordAppService.CreateCacheAsync(tradePairId, dto);
            }
        }
        
        [Fact]
        public async Task RevertResultTest()
        {
            var chainId = "tDVV";
            var blockHeight = 1L;
            var address = "2Ck7Hg4LD3LMHiKpbbPJuyVXv1zbFLzG7tP6ZmWf3L2ajwtSnk";
            var tradePairId = Guid.NewGuid();
            await _tradeRecordAppService.CreateCacheAsync(tradePairId, new SwapRecordDto()
            {
                ChainId = chainId,
                TransactionHash = "AAA",
                Sender = address,
                BlockHeight = blockHeight,
            });
            await _tradeRecordIndexRepository.AddAsync(new Index.TradeRecord()
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000000"),
                ChainId = chainId,
                TransactionHash = "AAA",
                Address = address,
                BlockHeight = blockHeight
            });
            _graphQlProvider.AddSwapRecord(new SwapRecordDto()
            {
                ChainId = chainId,
                TransactionHash = "AAA",
                Sender = address,
                BlockHeight = blockHeight,
            });
            
            await _tradeRecordAppService.CreateCacheAsync(tradePairId, new SwapRecordDto()
            {
                ChainId = chainId,
                TransactionHash = "BBB",
                Sender = address,
                BlockHeight = 2,
            });
            await _tradeRecordIndexRepository.AddAsync(new Index.TradeRecord()
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000000"),
                ChainId = chainId,
                TransactionHash = "BBB",
                Address = address,
                BlockHeight = 2
            });
            
            await _tradeRecordAppService.CreateCacheAsync(tradePairId, new SwapRecordDto()
            {
                ChainId = chainId,
                TransactionHash = "CCC",
                Sender = address,
                BlockHeight = 3,
            });
            await _tradeRecordIndexRepository.AddAsync(new Index.TradeRecord()
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000000"),
                ChainId = chainId,
                TransactionHash = "CCC",
                Address = address,
                BlockHeight = 3
            });
            
            
            await _tradeRecordAppService.RevertAsync(chainId);
            
            Thread.Sleep(3000);
            
            var trades = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = "tDVV"
            });
            trades.TotalCount.ShouldBe(2);
            
            var Confirmed = await _tradeRecordAppService.GetRecordAsync("AAA");
            Confirmed.ShouldNotBeNull();
            Confirmed.TransactionHash.ShouldBe("AAA");
            
            var reverted = await _tradeRecordAppService.GetRecordAsync("BBB");
            reverted.ShouldBeNull();
            
            var notConfirmed = await _tradeRecordAppService.GetRecordAsync("CCC");
            notConfirmed.ShouldNotBeNull();
            notConfirmed.TransactionHash.ShouldBe("CCC");
        }

        [Fact]
        public async Task CreateTest()
        {
            NewTradeRecordEvent recordEvent = null;
            _eventBus.Subscribe<NewTradeRecordEvent>(t =>
            {
                recordEvent = t;
                return Task.CompletedTask;
            });
            var recordInput = new TradeRecordCreateDto
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Address = "0x123456789",
                Side = TradeSide.Buy,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                Token0Amount = "100",
                Token1Amount = "1000",
                TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f",
                Channel = "TestChanel",
                Sender = "0x987654321"
            };
            await _tradeRecordAppService.CreateAsync(recordInput);
            
            var count = await _tradeRecordAppService.GetUserTradeAddressCountAsync(ChainId, TradePairEthUsdtId);
            count.ShouldBe(1);

            var record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = recordInput.ChainId,
                Address = recordInput.Address,
                TradePairId = recordInput.TradePairId,
                TransactionHash = recordInput.TransactionHash,
                Side = 0,
                Sorting = "timestamp asc",
                MaxResultCount = 10,
                TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            });
            record.Items.Count.ShouldBe(1);
            record.Items[0].ChainId.ShouldBe(recordInput.ChainId);
            record.Items[0].Address.ShouldBe(recordInput.Address);
            record.Items[0].Side.ShouldBe(recordInput.Side);
            record.Items[0].Timestamp.ShouldBe(recordInput.Timestamp);
            record.Items[0].Token0Amount.ShouldBe(recordInput.Token0Amount);
            record.Items[0].Token1Amount.ShouldBe(recordInput.Token1Amount);
            record.Items[0].TransactionHash.ShouldBe(recordInput.TransactionHash);
            record.Items[0].Price.ShouldBe(10);
            record.Items[0].Channel.ShouldBe(recordInput.Channel);
            record.Items[0].Sender.ShouldBe(recordInput.Sender);

            await CheckTradePairAsync(recordInput.TradePairId, record.Items[0].TradePair);
            
            var recordInput2 = new TradeRecordCreateDto
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Address = "0x12345678900",
                Side = TradeSide.Sell,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                Token0Amount = "200",
                Token1Amount = "4000",
                TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc",
                Channel = "TestChanel2",
                Sender = "0x9876543212"
            };
            await _tradeRecordAppService.CreateAsync(recordInput2);
            
            var record2 = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = recordInput2.ChainId,
                Address = recordInput2.Address,
                TradePairId = recordInput2.TradePairId,
                MaxResultCount = 10,
                TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            });
            record2.Items.Count.ShouldBe(1);
            record2.Items[0].ChainId.ShouldBe(recordInput2.ChainId);
            record2.Items[0].Address.ShouldBe(recordInput2.Address);
            record2.Items[0].Side.ShouldBe(recordInput2.Side);
            record2.Items[0].Timestamp.ShouldBe(recordInput2.Timestamp);
            record2.Items[0].Token0Amount.ShouldBe(recordInput2.Token0Amount);
            record2.Items[0].Token1Amount.ShouldBe(recordInput2.Token1Amount);
            record2.Items[0].TransactionHash.ShouldBe(recordInput2.TransactionHash);
            record2.Items[0].Price.ShouldBe(20);
            record2.Items[0].Channel.ShouldBe(recordInput2.Channel);
            record2.Items[0].Sender.ShouldBe(recordInput2.Sender);

            var record3 = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = recordInput2.ChainId,
                Address = recordInput2.Address,
                TradePairId = recordInput2.TradePairId,
                Side = 3
            });
            record3.Items.Count.ShouldBe(0);
        }

        [Fact]
        public async Task GetListTest()
        {
            var input1 = new TradeRecordCreateDto
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                Address = "0x123456789",
                Side = TradeSide.Buy,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-1)),
                Token0Amount = "100",
                Token1Amount = "1000",
                TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f"
            };
            await _tradeRecordAppService.CreateAsync(input1);
            
            var input2 = new TradeRecordCreateDto
            {
                ChainId = ChainId,
                TradePairId = TradePairBtcEthId,
                Address = "0x123456780",
                Side = TradeSide.Sell,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                Token0Amount = "10",
                Token1Amount = "100",
                TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f"
            };
            await _tradeRecordAppService.CreateAsync(input2);

            var record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Address = "0x",
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(0);
            record.Items.Count.ShouldBe(0);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(2);
            record.Items.Count.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Address = "0x123456789",
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(1);
            record.Items.Count.ShouldBe(1);
            record.Items[0].TradePair.Id.ShouldBe(TradePairEthUsdtId);

            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                TradePairId = TradePairEthUsdtId,
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(1);
            record.Items.Count.ShouldBe(1);
            record.Items[0].TradePair.Id.ShouldBe(TradePairEthUsdtId);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(2);
            record.Items.Count.ShouldBe(2);
            //record.Items[0].TradePair.Id.ShouldBe(TradePairEthUsdtId);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(2);
            record.Items.Count.ShouldBe(2);
            //record.Items[0].TradePair.Id.ShouldBe(TradePairBtcEthId);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                FeeRate = 0.5,
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(1);
            record.Items.Count.ShouldBe(1);
            record.Items[0].TradePair.Id.ShouldBe(TradePairEthUsdtId);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                TimestampMin = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-2)),
                TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-1)),
                MaxResultCount = 10,
            });
            record.TotalCount.ShouldBe(1);
            record.Items.Count.ShouldBe(1);
            record.Items[0].TradePair.Id.ShouldBe(TradePairEthUsdtId);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                TokenSymbol = "BSC",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(0);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "timestamp",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "tradepair",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "side",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "totalpriceinusd",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "timestamp desc",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "tradepair desc",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "side desc",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
            
            record = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                Sorting = "totalpriceinusd desc",
                MaxResultCount = 1,
            });
            record.TotalCount.ShouldBe(2);
        }

        [Fact]
        public async Task GetList_Page_Test()
        {
            for (int i = 0; i < 15; i++)
            {
                var input1 = new TradeRecordCreateDto
                {
                    ChainId = ChainId,
                    TradePairId = TradePairEthUsdtId,
                    Address = "0x123456789",
                    Side = TradeSide.Buy,
                    Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                    Token0Amount = "100",
                    Token1Amount = "1000",
                    TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f"
                };
                await _tradeRecordAppService.CreateAsync(input1);
            }
            
            var records = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                SkipCount = 0,
                MaxResultCount = 10,
            });
            records.TotalCount.ShouldBe(15);
            records.Items.Count.ShouldBe(10);
            
            records = await _tradeRecordAppService.GetListAsync(new GetTradeRecordsInput
            {
                ChainId = ChainId,
                SkipCount = 10,
                MaxResultCount = 10,
            });
            records.TotalCount.ShouldBe(15);
            records.Items.Count.ShouldBe(5);
        }
    }
}