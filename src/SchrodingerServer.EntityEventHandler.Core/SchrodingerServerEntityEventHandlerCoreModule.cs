using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SchrodingerServer.EntityEventHandler.Core.Options;
using StackExchange.Redis;
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
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SchrodingerServerEntityEventHandlerCoreModule>(); });
            var configuration = context.Services.GetConfiguration();
            ConfigureRateLimiting(context, configuration);
        }

        private void ConfigureRateLimiting(ServiceConfigurationContext context, IConfiguration configuration)
        {
            var multiplexer = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
            context.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            Configure<RateLimitOptions>(configuration.GetSection("RateLimitOptions"));
        }
    }
}