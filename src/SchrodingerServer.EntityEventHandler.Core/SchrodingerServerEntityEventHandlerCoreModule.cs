using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchrodingerServer.EntityEventHandler.Core.Options;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace SchrodingerServer.EntityEventHandler.Core
{
    [DependsOn(typeof(AbpAutoMapperModule),
        typeof(SchrodingerServerApplicationModule),
        typeof(SchrodingerServerApplicationContractsModule))]
    public class SchrodingerServerEntityEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<SchrodingerServerEntityEventHandlerCoreModule>();
            });
        }
    }
}