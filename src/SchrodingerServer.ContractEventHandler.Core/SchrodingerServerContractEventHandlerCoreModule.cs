using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace SchrodingerServer.ContractEventHandler.Core
{
    [DependsOn(
        typeof(AbpAutoMapperModule)
    )]
    public class SchrodingerServerContractEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<SchrodingerServerContractEventHandlerCoreModule>();
            });
        }
    }
}