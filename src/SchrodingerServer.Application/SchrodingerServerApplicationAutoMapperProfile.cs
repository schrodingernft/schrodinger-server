using AutoMapper;
using Google.Protobuf;
using SchrodingerServer.ContractInvoke.Eto;
using SchrodingerServer.Dtos.Faucets;
using SchrodingerServer.Grains.Grain.ContractInvoke;
using SchrodingerServer.Grains.Grain.Faucets;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Users.Eto;

namespace SchrodingerServer;

public class SchrodingerServerApplicationAutoMapperProfile : Profile
{
    public SchrodingerServerApplicationAutoMapperProfile()
    {
        CreateMap<UserSourceInput, UserGrainDto>().ReverseMap();
        CreateMap<UserGrainDto, UserInformationEto>().ReverseMap();
        CreateMap<FaucetsGrainDto, FaucetsTransferResultDto>();
        CreateMap<ContractInvokeGrainDto, ContractInvokeEto>()
            .ForMember(des => des.Param, opt
                => opt.MapFrom(source => JsonFormatter.Default.Format(source.Param)));
    }
}