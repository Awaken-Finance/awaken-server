using AutoMapper;
using AwakenServer.Grains.Grain.Chain;
using AwakenServer.Grains.Grain.Favorite;
using AwakenServer.Grains.Grain.Tokens;
using AwakenServer.Grains.State.Chain;
using AwakenServer.Grains.State.Favorite;
using AwakenServer.Grains.State.Tokens;
using AwakenServer.Grains.Grain.Favorite;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.Chain;
using AwakenServer.Grains.State.Favorite;
using AwakenServer.Grains.State.Trade;
using AwakenServer.Grains.State.Favorite;
using AwakenServer.Grains.State.Price;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using ChainState = AwakenServer.Grains.State.Chain.ChainState;

namespace AwakenServer.Grains;

public class AwakenServerGrainsAutoMapperProfile : Profile
{
    public AwakenServerGrainsAutoMapperProfile()
    {
        CreateMap<ChainState, ChainGrainDto>().ReverseMap();
        CreateMap<KLineState, KLineGrainDto>().ReverseMap();
        CreateMap<TokenState, TokenGrainDto>().ReverseMap();
        CreateMap<TokenCreateDto, TokenState>().ReverseMap();
        CreateMap<FavoriteGrainDto, FavoriteInfo>().ReverseMap();
        CreateMap<FavoriteInfo, FavoriteGrainDto>().ReverseMap();
        CreateMap<TradePairMarketDataSnapshotState, TradePairMarketDataSnapshotGrainDto>().ReverseMap();
        CreateMap<TradePairMarketDataSnapshotGrainDto, TradePairMarketDataSnapshotState>().ReverseMap();
        CreateMap<TradePairGrainDto, TradePairState>().ReverseMap();
        CreateMap<TradePairGrainDto, TradePairDto>().ReverseMap();
        CreateMap<TradeRecordGrainDto, TradeRecordState>().ReverseMap();
        CreateMap<TradeRecordGrainDto, TradeRecordIndexDto>().ReverseMap();
        CreateMap<UserTradeSummaryState, UserTradeSummaryGrainDto>().ReverseMap();
        CreateMap<UserTradeSummaryGrainDto, UserTradeSummaryState>().ReverseMap();
        CreateMap<UserLiquidityGrainDto, Liquidity>().ReverseMap();
        CreateMap<Token, TokenDto>().ReverseMap();
        CreateMap<LiquidityRecordGrainDto, LiquidityRecordState>().ReverseMap();
        CreateMap<SyncRecordsGrainDto, SyncRecordsState>().ReverseMap();
        CreateMap<UnconfirmedTransactionsGrainDto, ToBeConfirmRecord>().ReverseMap();
    }
}