using AElf.Client.Service;
using Microsoft.Extensions.DependencyInjection;
using SchrodingerServer.Common.ApplicationHandler;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace SchrodingerServer.Grains;

[DependsOn(
    typeof(AbpAutoMapperModule), typeof(SchrodingerServerApplicationContractsModule))]
public class SchrodingerServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SchrodingerServerGrainsModule>(); });
        context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
    }
}