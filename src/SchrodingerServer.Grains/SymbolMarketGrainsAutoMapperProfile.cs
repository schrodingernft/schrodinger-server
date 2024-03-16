using AutoMapper;
using SchrodingerServer.Dtos.Adopts;
using SchrodingerServer.Grains.Grain.Traits;
using SchrodingerServer.Grains.Grain.ContractInvoke;
using SchrodingerServer.Grains.Grain.Faucets;
using SchrodingerServer.Grains.Grain.Points;
using SchrodingerServer.Grains.State.ContractInvoke;
using SchrodingerServer.Grains.State.Faucets;
using SchrodingerServer.Grains.State.Points;
using SchrodingerServer.Grains.State.Users;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Users.Eto;

namespace SchrodingerServer.Grains;

public class SymbolMarketGrainsAutoMapperProfile : Profile
{
    public SymbolMarketGrainsAutoMapperProfile()
    {
        CreateMap<UserGrainDto, UserState>().ReverseMap();
        CreateMap<UserGrainDto, UserInformationEto>().ReverseMap();
        CreateMap<FaucetsState, FaucetsGrainDto>();
        CreateMap<AdoptImageInfoState, WaterImageGrainInfoDto>();
        CreateMap<ContractInvokeGrainDto, ContractInvokeState>().ReverseMap();;
        CreateMap<PointDailyRecordGrainDto, PointDailyRecordState>().ReverseMap();;
    }
}