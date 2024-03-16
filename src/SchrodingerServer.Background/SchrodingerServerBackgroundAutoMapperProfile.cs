using AutoMapper;
using SchrodingerServer.Background.Dtos;
using SchrodingerServer.ContractInvoke.Eto;
using SchrodingerServer.Grains.Grain.ContractInvoke;
using SchrodingerServer.Users.Index;

namespace SchrodingerServer.Background;

public class SchrodingerServerBackgroundAutoMapperProfile : Profile
{
    public SchrodingerServerBackgroundAutoMapperProfile()
    {
       // CreateMap<ContractInvokeGrainDto, ContractInvokeEto>();
    }
}