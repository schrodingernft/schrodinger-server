using AutoMapper;
using SchrodingerServer.Background.Dtos;
using SchrodingerServer.ContractInvoke.Eto;
using SchrodingerServer.ContractInvoke.Index;
using SchrodingerServer.Grains.Grain.ContractInvoke;
using SchrodingerServer.Users.Index;

namespace SchrodingerServer.Background;

public class SchrodingerServerBackgroundAutoMapperProfile : Profile
{
    public SchrodingerServerBackgroundAutoMapperProfile()
    {
        CreateMap<ContractInvokeIndex, ContractInvokeEto>();
        CreateMap<ContractInvokeEto, ContractInvokeGrainDto>().ReverseMap();
    }
}