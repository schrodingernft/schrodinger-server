using Microsoft.Extensions.DependencyInjection;
using SchrodingerServer.Grains;
using SchrodingerServer.Grains.Grain.ApplicationHandler;
using SchrodingerServer.Grains.Grain.Provider;
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
        var configuration = context.Services.GetConfiguration();
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<FaucetsTransferOptions>(configuration.GetSection("Faucets"));
        Configure<SyncTokenOptions>(configuration.GetSection("Sync"));
        context.Services.AddSingleton<IContractProvider, ContractProvider>();
    }
}