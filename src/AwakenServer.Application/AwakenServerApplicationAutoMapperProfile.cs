using System;
using AutoMapper;
using AwakenServer.Asset;
using AwakenServer.Chains;
using AwakenServer.Debits;
using AwakenServer.Debits.DebitAppDto;
using AwakenServer.Debits.Entities;
using AwakenServer.Debits.Entities.Es;
using AwakenServer.Dividend.DividendAppDto;
using AwakenServer.Dividend.Entities;
using AwakenServer.Dividend.Entities.Es;
using AwakenServer.Entities.GameOfTrust;
using AwakenServer.Entities.GameOfTrust.Es;
using AwakenServer.Farms;
using AwakenServer.Farms.Entities;
using AwakenServer.Farms.Entities.Es;
using AwakenServer.Favorite;
using AwakenServer.GameOfTrust.DTos;
using AwakenServer.GameOfTrust.DTos.Dto;
using AwakenServer.Grains.Grain.Chain;
using AwakenServer.Grains.Grain.Tokens;
using AwakenServer.Grains.Grain.Favorite;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Price.UserTradeSummary;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.Tokens;
using AwakenServer.IDO.Dtos;
using AwakenServer.IDO.Entities;
using AwakenServer.IDO.Entities.Es;
using AwakenServer.Price.Dtos;
using AwakenServer.Price.Etos;
using AwakenServer.Price.Index;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using AwakenServer.Trade.Etos;
using AwakenServer.Trade.Index;
using Volo.Abp.AutoMapper;
using KLine = AwakenServer.Trade.Index.KLine;
using Token = AwakenServer.Tokens.Token;
using TradePairMarketDataSnapshot = AwakenServer.Trade.Index.TradePairMarketDataSnapshot;
using TradeRecord = AwakenServer.Trade.TradeRecord;
using UserLiquidity = AwakenServer.Trade.Index.UserLiquidity;

namespace AwakenServer
{
    public class AwakenServerApplicationAutoMapperProfile : Profile
    {
        public AwakenServerApplicationAutoMapperProfile()
        {
            /* You can configure your AutoMapper mapping configuration here.
             * Alternatively, you can split your mapping configurations
             * into multiple profile classes for a better organization. */

            CreateMap<Chain, ChainDto>();
            CreateMap<ChainDto, Chain>();
            CreateMap<ChainCreateDto, Chain>();
            CreateMap<Chain, NewChainEvent>();
            CreateMap<NewChainEvent, Chain>();
            CreateMap<ChainDto, ChainResponseDto>();
            CreateMap<ChainDto, ChainCreateDto>();

            CreateMap<Chain, ChainGrainDto>();
            CreateMap<ChainGrainDto, ChainDto>();

            CreateMap<UserTokenDto, UserTokenInfo>();

            CreateMap<TokenGrainDto, Tokens.TokenDto>();
            CreateMap<TokenGrainDto, Token>();
            CreateMap<TokenGrainDto, NewTokenEvent>();
            CreateMap<TokenCreateDto, TokenState>();
            CreateMap<NewTokenEvent, Token>();
            CreateMap<Tokens.TokenDto, Token>();
            CreateMap<TokenCreateDto, Token>();
            CreateMap<Token, TokenCreateDto>();
            CreateMap<Token, Tokens.TokenDto>();

            CreateMap<TokenCreateDto, Token>().Ignore(x => x.Id);

            CreateMap<TradePairCreateDto, Trade.TradePair>().Ignore(x => x.Id);
            CreateMap<TradeRecordCreateDto, Trade.TradeRecord>().Ignore(x => x.Id).ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.FromUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<TradeRecord, TradeRecordGrainDto>();
            CreateMap<TradeRecord, TradeRecordEto>();
            CreateMap<UserTradeSummaryGrainDto, UserTradeSummaryEto>();
            CreateMap<UserTradeSummaryEto, Trade.Index.UserTradeSummary>();
            CreateMap<Trade.TradePair, TradePairDto>();
            CreateMap<Trade.Index.TradePair, TradePairIndexDto>();
            CreateMap<Trade.Index.TradePair, TradePairDto>()
                .ForMember(dest => dest.Token0Symbol, opt => opt.MapFrom(src => src.Token0.Symbol))
                .ForMember(dest => dest.Token1Symbol, opt => opt.MapFrom(src => src.Token1.Symbol))
                .ForMember(dest => dest.Token0Id, opt => opt.MapFrom(src => src.Token0.Id))
                .ForMember(dest => dest.Token1Id, opt => opt.MapFrom(src => src.Token1.Id));
            CreateMap<TradePairEto, Trade.Index.TradePair>();

            CreateMap<Trade.TradePair, TradePairWithToken>();
            CreateMap<TradePairWithToken, TradePairWithTokenDto>();

            CreateMap<TradePairMarketDataSnapshotEto, TradePairMarketDataSnapshot>();


            CreateMap<TradePairInfoDto, TradePairDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)));
            CreateMap<TradePairInfoDto, Trade.Index.TradePair>().Ignore(x => x.ChainId)
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.Parse(src.Id)));
            
            CreateMap<TradePairInfoIndex, TradePairInfoDto>();
            CreateMap<TradePairInfoIndex, TradePairDto>();
            CreateMap<TradePairInfoIndex, TradePairDto>();
            CreateMap<TradePairInfoIndex, Trade.TradePair>();
            CreateMap<TradePairCreateDto, TradePairInfoIndex>();
            CreateMap<TradePairCreateDto, Trade.Index.TradePair>();
            CreateMap<TradePairDto, Trade.TradePair>();
            CreateMap<GetTradePairByIdsInput, GetTradePairsInput>();

            CreateMap<LiquidityRecordCreateDto, Trade.LiquidityRecord>().Ignore(x => x.Id).ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.FromUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<Trade.LiquidityRecord, NewLiquidityRecordEvent>();
            CreateMap<LiquidityRecordEto, Trade.Index.LiquidityRecord>();
            CreateMap<Trade.Index.LiquidityRecord, LiquidityRecordIndexDto>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<GetLiquidityRecordsInput, GetLiquidityRecordIndexInput>();
            CreateMap<UserLiquidityEto, UserLiquidity>();
            CreateMap<LiquidityRecordDto, LiquidityRecordIndexDto>().Ignore(x => x.ChainId);
            CreateMap<UserLiquidityDto, UserLiquidityIndexDto>();
            
            CreateMap<GetUserAssertInput, GetUserLiquidityInput>();
            CreateMap<Trade.Index.UserLiquidity, UserLiquidityIndexDto>();

            CreateMap<Trade.TradeRecord, NewTradeRecordEvent>();
            CreateMap<TradeRecordEto, Trade.Index.TradeRecord>();
            CreateMap<Trade.Index.TradeRecord, TradeRecordIndexDto>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<KLineEto, KLine>();
            CreateMap<Trade.Index.KLine, KLineDto>();
            CreateMap<KLineGrainDto, KLineEto>();

            //Price
            CreateMap<LendingTokenPriceCreateOrUpdateDto, Price.LendingTokenPrice>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.FromUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<Price.LendingTokenPrice, LendingTokenPriceDto>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<Price.OtherLpToken, OtherLpTokenDto>();
            CreateMap<OtherLpTokenCreateDto, Price.OtherLpToken>();
            CreateMap<OtherLpTokenDto, Price.OtherLpToken>();
            CreateMap<LendingTokenPriceEto, Price.Index.LendingTokenPrice>();
            CreateMap<LendingTokenPriceEto, LendingTokenPriceHistory>()
                .Ignore(d => d.Id)
                .Ignore(d => d.Timestamp);
            CreateMap<Price.Index.LendingTokenPrice, LendingTokenPriceIndexDto>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<LendingTokenPriceHistory, LendingTokenPriceHistoryIndexDto>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<OtherLpTokenEto, Price.Index.OtherLpToken>();
            CreateMap<Price.Index.OtherLpToken, OtherLpTokenIndexDto>();
            CreateMap<TradePairMarketDataSnapshot, AwakenServer.Trade.TradePairMarketDataSnapshot>();
            CreateMap<AwakenServer.Trade.TradePairMarketDataSnapshot, TradePairMarketDataSnapshotGrainDto>();
            CreateMap<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotEto>();

            //Farm
            CreateMapForFarm();

            //Debit
            CreateMapForDebit();

            //GameOfTrust
            CreateMapForGameOfTrust();

            //IDO
            CreateMapForIDO();

            //Dividend
            CreateMapForDividend();
            
            //Favorite
            CreateMapForFavorite();
        }

        private void CreateMapForGameOfTrust()
        {
            // game of trust
            CreateMap<Entities.GameOfTrust.Es.GameOfTrust, GameOfTrustDto>();
            CreateMap<GameOfTrustRecord, GetUserGameOfTrustRecordDto>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<Token, GameOfTrust.DTos.Dto.TokenDto>();
            CreateMap<GameOfTrustMarketData, MarketDataDto>().ForMember(
                destination => destination.Timestamp,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.Timestamp)));
            CreateMap<Entities.GameOfTrust.Es.GameOfTrust, MarketCapsDto>();
            CreateMap<UserGameOfTrust, UserGameofTrustDto>();
            CreateMap<GameOfTrustWithToken, GameOfTrustDto>();
        }

        private void CreateMapForFarm()
        {
            CreateMap<Farm, FarmDto>();
            CreateMap<FarmPool, FarmPoolDto>();
            CreateMap<FarmUserInfo, FarmUserInfoDto>();
            CreateMap<FarmRecord, FarmRecordDto>().ForMember(dest => dest.Timestamp,
                opts => opts.MapFrom(src => DateTimeHelper.ToUnixTimeMilliseconds(src.Date)));
            CreateMap<FarmBase, FarmBaseDto>();
            CreateMap<FarmPoolBase, FarmPoolBaseDto>();
            CreateMap<Token, FarmTokenDto>();
        }

        private void CreateMapForDebit()
        {
            CreateMap<CompController, CompControllerDto>();
            CreateMap<CToken, CTokenDto>();
            CreateMap<CTokenRecord, CTokenRecordDto>().ForMember(dest => dest.Timestamp,
                opts => opts.MapFrom(src => DateTimeHelper.ToUnixTimeMilliseconds(src.Date)));
            CreateMap<CTokenUserInfo, CTokenUserInfoDto>();
            CreateMap<CompControllerBase, CompControllerBaseDto>();
            CreateMap<CTokenBase, CTokenBaseDto>();
            CreateMap<Token, DebitTokenDto>();
        }

        private void CreateMapForIDO()
        {
            CreateMap<PublicOffering, PublicOfferingDto>().ForMember(dest => dest.StartTimestamp,
                    opts => opts.MapFrom(src => DateTimeHelper.ToUnixTimeMilliseconds(src.StartTime)))
                .ForMember(dest => dest.EndTimestamp,
                    opts => opts.MapFrom(src => DateTimeHelper.ToUnixTimeMilliseconds(src.EndTime)));
            CreateMap<PublicOfferingWithToken, PublicOfferingWithTokenDto>();
            CreateMap<PublicOfferingRecord, PublicOfferingRecordDto>().ForMember(dest => dest.DateTime,
                opts => opts.MapFrom(src => DateTimeHelper.ToUnixTimeMilliseconds(src.DateTime)));
            CreateMap<UserPublicOffering, UserPublicOfferingDto>();
        }

        private void CreateMapForDividend()
        {
            CreateMap<Dividend.Entities.Dividend, DividendDto>();
            CreateMap<DividendToken, DividendTokenDto>();
            CreateMap<DividendPool, DividendPoolDto>();
            CreateMap<DividendPoolToken, DividendPoolTokenDto>();
            CreateMap<DividendUserPool, DividendUserPoolDto>();
            CreateMap<DividendUserToken, DividendUserTokenDto>();
            CreateMap<DividendUserRecord, DividendUserRecordDto>().ForMember(dest => dest.Date,
                opts => opts.MapFrom(src => DateTimeHelper.ToUnixTimeMilliseconds(src.DateTime)));
            CreateMap<DividendPoolBaseInfo, DividendPoolBaseInfoDto>();
            CreateMap<DividendBase, DividendBaseDto>();
        }
        
        private void CreateMapForFavorite()
        {
            CreateMap<FavoriteCreateDto, FavoriteGrainDto>();
            CreateMap<FavoriteGrainDto, FavoriteDto>();
        }
    }
}