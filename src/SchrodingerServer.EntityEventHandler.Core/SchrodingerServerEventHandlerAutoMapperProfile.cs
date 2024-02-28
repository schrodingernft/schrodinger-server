using System.Linq;
using AutoMapper;
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
    }
}