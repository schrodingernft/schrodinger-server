using Microsoft.Extensions.DependencyInjection;
using SchrodingerServer.Grains;
using SchrodingerServer.Grains.Grain.ApplicationHandler;
using SchrodingerServer.MongoDB;
using SchrodingerServer.Options;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using ChainOptions = SchrodingerServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace SchrodingerServer.Silo;

[DependsOn(
    typeof(SchrodingerServerGrainsModule),
    typeof(AbpAspNetCoreMvcUiMultiTenancyModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(SchrodingerServerApplicationModule),
    typeof(SchrodingerServerMongoDbModule),
    typeof(AbpAutofacModule)
)]
public class SchrodingerServerOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SchrodingerServerOrleansSiloModule>(); });
        context.Services.AddHostedService<SchrodingerServerHostedService>();
        var configuration = context.Services.GetConfiguration();
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<SchrodingerServer.Options.ChainOptions>(configuration.GetSection("Chains"));
        Configure<FaucetsTransferOptions>(configuration.GetSection("Faucets"));
        Configure<SecurityServerOptions>(configuration.GetSection("SecurityServer"));
        
        context.Services.AddHttpClient();
        // context.Services.AddSingleton<IContractProvider, ContractProvider>();
    }
}