using AutoMapper;
using AwakenServer.Asset;
using AwakenServer.Grains.Grain.Chain;
using AwakenServer.Grains.Grain.Favorite;
using AwakenServer.Grains.Grain.Tokens;
using AwakenServer.Grains.State.Chain;
using AwakenServer.Grains.State.Favorite;
using AwakenServer.Grains.State.Tokens;
using AwakenServer.Grains.Grain.Favorite;
using AwakenServer.Grains.Grain.MyPortfolio;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Price.TradeRecord;
using AwakenServer.Grains.Grain.StatInfo;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.State.Chain;
using AwakenServer.Grains.State.Favorite;
using AwakenServer.Grains.State.Trade;
using AwakenServer.Grains.State.Favorite;
using AwakenServer.Grains.State.MyPortfolio;
using AwakenServer.Grains.State.Price;
using AwakenServer.Grains.State.StatInfo;
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
        CreateMap<TradePairGrainDto, PositionTradePairDto>()
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.ToString()))
            .ForMember(dest => dest.TVL, opt => opt.MapFrom(src => src.TVL.ToString()))
            .ForMember(dest => dest.Volume24h, opt => opt.MapFrom(src => src.Volume24h.ToString()));
        CreateMap<TradeRecordGrainDto, TradeRecordState>().ReverseMap();
        CreateMap<TradeRecordGrainDto, TradeRecordIndexDto>().ReverseMap();
        CreateMap<UserTradeSummaryState, UserTradeSummaryGrainDto>().ReverseMap();
        CreateMap<UserTradeSummaryGrainDto, UserTradeSummaryState>().ReverseMap();
        CreateMap<Token, TokenDto>().ReverseMap();
        CreateMap<LiquidityRecordGrainDto, LiquidityRecordState>()
            .ForMember(dest => dest.PairAddress, opt => opt.MapFrom(src => src.Pair));
        CreateMap<LiquidityRecordState, LiquidityRecordGrainDto>()
            .ForMember(dest => dest.Pair, opt => opt.MapFrom(src => src.PairAddress));
        CreateMap<SyncRecordsGrainDto, SyncRecordsState>().ReverseMap();
        CreateMap<UnconfirmedTransactionsGrainDto, ToBeConfirmRecord>().ReverseMap();
        CreateMap<CurrentTradePairState, CurrentTradePairGrainDto>().ReverseMap();
        CreateMap<CurrentUserLiquidityState, CurrentUserLiquidityGrainDto>().ReverseMap();
        CreateMap<UserLiquiditySnapshotState, UserLiquiditySnapshotGrainDto>().ReverseMap();
        CreateMap<StatInfoSnapshotState, StatInfoSnapshotGrainDto>().ReverseMap();
        CreateMap<GlobalStatInfoState, GlobalStatInfoGrainDto>().ReverseMap();
        CreateMap<PoolStatInfoState, PoolStatInfoGrainDto>().ReverseMap();
        CreateMap<TokenStatInfoState, TokenStatInfoGrainDto>().ReverseMap();
    }
}