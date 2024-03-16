using AutoMapper;
using SchrodingerServer.ContractInvoke.Eto;
using SchrodingerServer.ContractInvoke.Index;
using SchrodingerServer.Grains.Grain.ContractInvoke;

namespace SchrodingerServer.ContractEventHandler.Core
{
    public class ContractEventHandlerAutoMapperProfile : Profile
    {
        public ContractEventHandlerAutoMapperProfile()
        {
            CreateMap<ContractInvokeIndex, ContractInvokeEto>();
           // CreateMap<ContractInvokeEto, ContractInvokeGrainDto>().ReverseMap();
        }
    }
}