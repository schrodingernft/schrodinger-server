using System.Linq;
using AutoMapper;
using Google.Protobuf;
using SchrodingerServer.ContractInvoke.Eto;
using SchrodingerServer.ContractInvoke.Index;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Eto;
using SchrodingerServer.Users.Index;


namespace SchrodingerServer.EntityEventHandler.Core;

public class SchrodingerServerEventHandlerAutoMapperProfile : Profile
{
    public SchrodingerServerEventHandlerAutoMapperProfile()
    {
        CreateMap<UserGrainDto, UserIndex>();
        CreateMap<UserInformationEto, UserIndex>()
            .ForMember(d => d.CaAddressListSide,
                opt => opt.MapFrom(src =>
                    src.CaAddressSide.Select(kv => new UserAddress { ChainId = kv.Key, Address = kv.Value })))
            .ReverseMap();
        CreateMap<ContractInvokeEto, ContractInvokeIndex>();
        CreateMap<HolderDailyChangeDto, HolderBalanceIndex>()
            .ForMember(des => des.BizDate, opt
                => opt.MapFrom(source => source.Date));
        CreateMap<PointDailyRecordEto, PointDailyRecordIndex>();
    }
}