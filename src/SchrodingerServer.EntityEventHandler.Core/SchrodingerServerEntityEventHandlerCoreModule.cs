using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            context.Services.AddSingleton<IHostedService, InitJobsService>();
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<SchrodingerServerEntityEventHandlerCoreModule>();
            });
        }
    }
}