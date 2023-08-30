using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Price.Dtos;
using AwakenServer.Price.Index;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class LendingTokenPriceAppServiceTests : PriceTestBase
    {
        private readonly ILendingTokenPriceAppService _lendingTokenPriceAppService;
        private readonly ILendingTokenPriceRepository _lendingTokenPriceRepository;
        private readonly INESTReaderRepository<Index.LendingTokenPrice, Guid> _lendingTokenIndexRepository;
        private readonly INESTReaderRepository<LendingTokenPriceHistory, Guid> _lendingTokenPriceHistoryRepository;

        public LendingTokenPriceAppServiceTests()
        {
            _lendingTokenPriceAppService = GetRequiredService<ILendingTokenPriceAppService>();
            _lendingTokenPriceRepository = GetRequiredService<ILendingTokenPriceRepository>();
            _lendingTokenIndexRepository = GetRequiredService<INESTReaderRepository<Index.LendingTokenPrice, Guid>>();
            _lendingTokenPriceHistoryRepository =
                GetRequiredService<INESTReaderRepository<LendingTokenPriceHistory, Guid>>();

        }
        
        private const long MillisecondsPerDay = 86400000;

        [Fact(Skip = "no need")]
        public async Task CreateOrUpdateTest()
        {
            //Create lendingTokenPrice and lendingTokenPriceHistory
            var createOrUpdateDto = new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = ChainId,
                TokenId = TokenBtcId,
                Price = "63678.72",
                PriceValue = 63678.72,
                Timestamp = Timestamp,
                BlockNumber = 100
            };
            await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);

            var lendingTokenPrice =
                await _lendingTokenPriceRepository.GetAsync(l => l.ChainId == ChainId && l.TokenId == TokenBtcId);
            CheckLendingTokenPrice(lendingTokenPrice, createOrUpdateDto);
            
            var lendingTokenPriceIndex = await _lendingTokenIndexRepository.GetAsync(lendingTokenPrice.Id);
            CheckLendingTokenPriceIndex(lendingTokenPriceIndex, createOrUpdateDto);
            
            var list = await _lendingTokenPriceHistoryRepository.GetListAsync();
            list.Item1.ShouldBe(1);
            var lendingTokenPriceHistory = list.Item2[0];
            lendingTokenPriceHistory.Id.ShouldNotBe(lendingTokenPrice.Id);
            CheckLendingTokenPriceHistory(lendingTokenPriceHistory, createOrUpdateDto);
            
            //Update lendingTokenPrice and lendingTokenPriceHistory
            {
                createOrUpdateDto.Price = "63787.7";
                createOrUpdateDto.PriceValue = 63787.7;
                createOrUpdateDto.Timestamp = 1636784479000;
                createOrUpdateDto.BlockNumber++;
                await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);
                lendingTokenPrice = await _lendingTokenPriceRepository.GetAsync(lendingTokenPrice.Id);
                CheckLendingTokenPrice(lendingTokenPrice, createOrUpdateDto);
                
                lendingTokenPriceIndex = await _lendingTokenIndexRepository.GetAsync(lendingTokenPrice.Id);
                CheckLendingTokenPriceIndex(lendingTokenPriceIndex, createOrUpdateDto);

                lendingTokenPriceHistory =
                    await _lendingTokenPriceHistoryRepository.GetAsync(list.Item2[0].Id);
                CheckLendingTokenPriceHistory(lendingTokenPriceHistory, createOrUpdateDto);
            
                list = await _lendingTokenPriceHistoryRepository.GetListAsync();
                list.Item1.ShouldBe(1);
                list.Item2[0].ShouldBeEquivalentTo(lendingTokenPriceHistory); 
            }

            //Update lendingTokenPrice, not update lendingTokenPriceHistory with invalid timestamp
            {
                createOrUpdateDto.Timestamp--;
                createOrUpdateDto.BlockNumber++;
                await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);
                
                lendingTokenPriceIndex = await _lendingTokenIndexRepository.GetAsync(lendingTokenPrice.Id);
                CheckLendingTokenPriceIndex(lendingTokenPriceIndex, createOrUpdateDto);

                var newLendingTokenPriceHistory = await _lendingTokenPriceHistoryRepository.GetAsync(lendingTokenPriceHistory.Id);
                newLendingTokenPriceHistory.ShouldBeEquivalentTo(lendingTokenPriceHistory);
            }
            
            //Update lendingTokenPrice, not update lendingTokenPriceHistory with invalid blockNumber
            {
                createOrUpdateDto.Timestamp++;
                createOrUpdateDto.BlockNumber--;
                await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);
                
                lendingTokenPriceIndex = await _lendingTokenIndexRepository.GetAsync(lendingTokenPrice.Id);
                CheckLendingTokenPriceIndex(lendingTokenPriceIndex, createOrUpdateDto);

                var newLendingTokenPriceHistory = await _lendingTokenPriceHistoryRepository.GetAsync(lendingTokenPriceHistory.Id);
                newLendingTokenPriceHistory.ShouldBeEquivalentTo(lendingTokenPriceHistory);
            }
            
            //Update lendingTokenPrice for another lendingTokenPriceHistory
            {
                createOrUpdateDto.Timestamp = createOrUpdateDto.Timestamp - createOrUpdateDto.Timestamp % 86400000 + 86400000;
                createOrUpdateDto.BlockNumber++;
                await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);
                
                list = await _lendingTokenPriceHistoryRepository.GetListAsync();
                list.Item1.ShouldBe(2);
                list.Item2[0].ShouldBeEquivalentTo(lendingTokenPriceHistory);
                list.Item2[1].Id.ShouldNotBe(lendingTokenPriceHistory.Id);
                CheckLendingTokenPriceHistory(list.Item2[1], createOrUpdateDto);
            }
            
           
            //Update lendingTokenPrice with multi change  
            // {
            //     createOrUpdateDto.Timestamp = createOrUpdateDto.Timestamp - createOrUpdateDto.Timestamp % 86400000 + 86400000;
            //     createOrUpdateDto.BlockNumber++;
            //     await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);
            //     
            //     createOrUpdateDto.Timestamp++;
            //     createOrUpdateDto.BlockNumber++;
            //     await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);
            //
            //     list = await _lendingTokenPriceHistoryRepository.GetListAsync();
            //     list.Item1.ShouldBe(3);  // TODO There is a problem that create and update index in es search quickly.
            // }
        }

        [Fact(Skip = "no need")]
        public async Task GetByTokenIdTest()
        {
            var lendingTokenPrice = await _lendingTokenPriceAppService.GetByTokenIdAsync(TokenBtcId);
            lendingTokenPrice.ShouldBeNull();
            var createOrUpdateDto = new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = ChainId,
                TokenId = TokenBtcId,
                Price = "63678.72",
                PriceValue = 63678.72,
                Timestamp = Timestamp,
                BlockNumber = 100
            };
            await _lendingTokenPriceAppService.CreateOrUpdateAsync(createOrUpdateDto);

            lendingTokenPrice = await _lendingTokenPriceAppService.GetByTokenIdAsync(TokenBtcId);
            lendingTokenPrice.Price.ShouldBe(createOrUpdateDto.Price);
            lendingTokenPrice.PriceValue.ShouldBe(createOrUpdateDto.PriceValue);
            lendingTokenPrice.Timestamp.ShouldBe(createOrUpdateDto.Timestamp);
            lendingTokenPrice.BlockNumber.ShouldBe(createOrUpdateDto.BlockNumber);
            lendingTokenPrice.ChainId.ShouldBe(createOrUpdateDto.ChainId);
            lendingTokenPrice.TokenId.ShouldBe(createOrUpdateDto.TokenId);
        }

        [Fact(Skip = "no need")]
        public async Task GetPricesWithTokenIdsTest()
        {
            // Get empty list
            var tokenIds = new List<Guid> {TokenBtcId, TokenEthId};
            var prices = await _lendingTokenPriceAppService.GetPricesAsync(new GetPricesInput
            {
                ChainId = ChainId,
                TokenIds = tokenIds.ToArray()
            });
            prices.Count.ShouldBe(0);
            
            //Get list
            var lendingTokenPriceInputs = await CreateLendingTokenPrices();
            
            prices = await _lendingTokenPriceAppService.GetPricesAsync(new GetPricesInput
            {
                ChainId = ChainId,
                TokenIds = tokenIds.ToArray()
            });
            prices.Count.ShouldBe(2);
            CheckLendingTokenPriceIndexDto(prices[0], lendingTokenPriceInputs[0]);
            CheckLendingTokenPriceIndexDto(prices[1], lendingTokenPriceInputs[1]);
            
            //Get with invalid input
            for (var i = 0; i < 1100; i++)
            {
                tokenIds.Add(TokenBtcId);
            }

            try
            {
                await _lendingTokenPriceAppService.GetPricesAsync(new GetPricesInput
                {
                    ChainId = ChainId,
                    TokenIds = tokenIds.ToArray()
                });
            }
            catch (AbpValidationException e)
            {
                e.ValidationErrors.Count.ShouldBe(1);
                e.ValidationErrors[0].ErrorMessage.ShouldBe("The field TokenIds must be a string or array type with a maximum length of '1000'.");
            }
        }

        [Fact(Skip = "no need")]
        public async Task GetPricesWithAddressesTest()
        {
            // Get empty list
            var tokenAddresses = new List<string> {TokenBtc.Address, TokenEth.Address};
            var prices = await _lendingTokenPriceAppService.GetPricesAsync(ChainId, tokenAddresses.ToArray());
            prices.Count.ShouldBe(0);
            
            //Get list
            var lendingTokenPriceInputs = await CreateLendingTokenPrices();
            
            prices = await _lendingTokenPriceAppService.GetPricesAsync(ChainId, tokenAddresses.ToArray());
            prices.Count.ShouldBe(2);
            CheckLendingTokenPriceIndexDto(prices[0], lendingTokenPriceInputs[0]);
            CheckLendingTokenPriceIndexDto(prices[1], lendingTokenPriceInputs[1]);
        }

        [Fact(Skip = "no need")]
        public async Task GetPriceHistoryTest()
        {
            var date = Timestamp - Timestamp % MillisecondsPerDay;
            // Get empty list
            var priceHistories = await _lendingTokenPriceAppService.GetPriceHistoryAsync(new GetPriceHistoryInput
            {
                ChainId = ChainId,
                TokenId = TokenBtcId,
                TimestampMin = date,
                TimestampMax = date + MillisecondsPerDay
            });
            priceHistories.TotalCount.ShouldBe(0);
            
            // Get list
            var lendingTokenPriceInputs = await CreateLendingTokenPrices();
            lendingTokenPriceInputs[0].Timestamp = Timestamp + MillisecondsPerDay;
            await _lendingTokenPriceAppService.CreateOrUpdateAsync(lendingTokenPriceInputs[0]);
            
            priceHistories = await _lendingTokenPriceAppService.GetPriceHistoryAsync(new GetPriceHistoryInput
            {
                ChainId = ChainId,
                TokenId = TokenBtcId,
                TimestampMin = date,
                TimestampMax = date + MillisecondsPerDay
            });
            priceHistories.TotalCount.ShouldBe(2);
            
            priceHistories = await _lendingTokenPriceAppService.GetPriceHistoryAsync(new GetPriceHistoryInput
            {
                ChainId = ChainId,
                TokenId = TokenBtcId,
                TimestampMin = date,
                TimestampMax = date + 1
            });
            priceHistories.TotalCount.ShouldBe(1);
        }

        private async Task<List<LendingTokenPriceCreateOrUpdateDto>> CreateLendingTokenPrices()
        {
            var btcPrice = new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = ChainId,
                TokenId = TokenBtcId,
                Price = "63678.72",
                PriceValue = 63678.72,
                Timestamp = Timestamp,
                BlockNumber = 100
            };
            await _lendingTokenPriceAppService.CreateOrUpdateAsync(btcPrice);
            
            var ethPrice = new LendingTokenPriceCreateOrUpdateDto
            {
                ChainId = ChainId,
                TokenId = TokenEthId,
                Price = "4611.03",
                PriceValue = 4611.03,
                Timestamp = Timestamp,
                BlockNumber = 100
            };
            await _lendingTokenPriceAppService.CreateOrUpdateAsync(ethPrice);
            return new List<LendingTokenPriceCreateOrUpdateDto> {btcPrice, ethPrice};
        }

        private void CheckLendingTokenPriceIndexDto(LendingTokenPriceIndexDto source,
            LendingTokenPriceCreateOrUpdateDto target)
        {
            source.ChainId.ShouldBe(target.ChainId);
            source.Token.Id.ShouldBe(target.TokenId);
            source.Price.ShouldBe(target.Price);
            source.Timestamp.ShouldBe(target.Timestamp);
        }

        private void CheckLendingTokenPrice(LendingTokenPrice lendingTokenPrice, LendingTokenPriceCreateOrUpdateDto dto)
        {
            lendingTokenPrice.ChainId.ShouldBe(dto.ChainId);
            lendingTokenPrice.TokenId.ShouldBe(dto.TokenId);
            lendingTokenPrice.Price.ShouldBe(dto.Price);
            lendingTokenPrice.PriceValue.ShouldBe(dto.PriceValue);
            lendingTokenPrice.Timestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp));
            lendingTokenPrice.BlockNumber.ShouldBe(dto.BlockNumber);
        }

        private void CheckLendingTokenPriceIndex(Index.LendingTokenPrice lendingTokenPriceIndex,
            LendingTokenPriceCreateOrUpdateDto dto)
        {
            lendingTokenPriceIndex.ChainId.ShouldBe(dto.ChainId);
            lendingTokenPriceIndex.Token.ShouldBeEquivalentTo(TokenBtc);
            lendingTokenPriceIndex.Price.ShouldBe(dto.Price);
            lendingTokenPriceIndex.PriceValue.ShouldBe(dto.PriceValue);
            lendingTokenPriceIndex.Timestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp));
            lendingTokenPriceIndex.BlockNumber.ShouldBe(dto.BlockNumber);
        }
        
        private void CheckLendingTokenPriceHistory(LendingTokenPriceHistory lendingTokenPriceHistory,
            LendingTokenPriceCreateOrUpdateDto dto)
        {
            lendingTokenPriceHistory.UpdateTimestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp));
            lendingTokenPriceHistory.Price.ShouldBe(dto.Price);
            lendingTokenPriceHistory.Timestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(dto.Timestamp - dto.Timestamp % 86400000));
            lendingTokenPriceHistory.Token.ShouldBeEquivalentTo(TokenBtc);
            lendingTokenPriceHistory.BlockNumber.ShouldBe(dto.BlockNumber);
            lendingTokenPriceHistory.ChainId.ShouldBe(dto.ChainId);
            lendingTokenPriceHistory.PriceValue.ShouldBe(dto.PriceValue);
        }
    }
}