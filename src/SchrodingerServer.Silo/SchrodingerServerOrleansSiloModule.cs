using Microsoft.Extensions.DependencyInjection;
using SchrodingerServer.Grains;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace SchrodingerServer.Silo;
[DependsOn(typeof(AbpAutofacModule),
    typeof(SchrodingerServerGrainsModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(AbpAspNetCoreSerilogModule)
)]
public class SchrodingerServerOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<SchrodingerServerHostedService>();
    }
}