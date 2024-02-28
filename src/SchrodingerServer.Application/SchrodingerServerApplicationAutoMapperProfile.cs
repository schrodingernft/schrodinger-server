using AutoMapper;
using SchrodingerServer.Users;
using SchrodingerServer.Users.Dto;
using SchrodingerServer.Users.Eto;
using SchrodingerServer.Users.Index;

namespace SchrodingerServer;

public class SchrodingerServerApplicationAutoMapperProfile : Profile
{
    public SchrodingerServerApplicationAutoMapperProfile()
    {
        CreateMap<UserSourceInput, UserGrainDto>().ReverseMap();
        CreateMap<UserGrainDto, UserInformationEto>().ReverseMap();
    }
}