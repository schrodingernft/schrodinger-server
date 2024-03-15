using AutoMapper;
using SchrodingerServer.ContractInvoke.Eto;
using SchrodingerServer.ContractInvoke.Index;

namespace SchrodingerServer.ContractEventHandler.Core
{
    public class ContractEventHandlerAutoMapperProfile : Profile
    {
        public ContractEventHandlerAutoMapperProfile()
        {
            CreateMap<ContractInvokeIndex, ContractInvokeEto>();
        }
    }
}