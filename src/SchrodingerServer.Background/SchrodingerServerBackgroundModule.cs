using System;
using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.CosmosDB;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using SchrodingerServer.Background.Services;
using SchrodingerServer.Background.Workers;
using SchrodingerServer.Common;
using SchrodingerServer.Grains;
using SchrodingerServer.MongoDB;
using SchrodingerServer.Options;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundJobs.Hangfire;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using Polly;

namespace SchrodingerServer.Background;

[DependsOn(
    typeof(SchrodingerServerApplicationContractsModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AbpAutofacModule),
    typeof(SchrodingerServerGrainsModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(SchrodingerServerDomainModule),
    typeof(SchrodingerServerMongoDbModule),
    typeof(AbpBackgroundJobsHangfireModule)
)]
public class SchrodingerServerBackgroundModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SchrodingerServerBackgroundModule>(); });

        var configuration = context.Services.GetConfiguration();
        Configure<ZealyUserOptions>(configuration.GetSection("ZealyUser"));
        Configure<UpdateScoreOptions>(configuration.GetSection("UpdateScore"));
        Configure<ZealyScoreOptions>(configuration.GetSection("ZealyScore"));

        context.Services.AddHostedService<SchrodingerServerHostService>();
        context.Services.AddHttpClient();
        ConfigureHangfire(context, configuration);
        ConfigureZealyClient(context, configuration);
        ConfigureOrleans(context, configuration);
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(SchrodingerServerGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }

    private void ConfigureZealyClient(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddHttpClient(CommonConstant.ZealyClientName, httpClient =>
        {
            httpClient.BaseAddress = new Uri(configuration["Zealy:BaseUrl"]);
            httpClient.DefaultRequestHeaders.Add(
                CommonConstant.ZealyApiKeyName, configuration["Zealy:ApiKey"]);
        }).AddTransientHttpErrorPolicy(policyBuilder =>
            policyBuilder.WaitAndRetryAsync(
                3, retryNumber => TimeSpan.FromMilliseconds(50)));;
    }

    private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
    {
        var mongoType = configuration["Hangfire:MongoType"];
        var connectionString = configuration["Hangfire:ConnectionString"];
        if (connectionString.IsNullOrEmpty()) return;

        if (mongoType.IsNullOrEmpty() ||
            mongoType.Equals(MongoType.MongoDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(x =>
            {
                x.UseMongoStorage(connectionString, new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    CheckConnection = true,
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
                });
            });
        }
        else if (mongoType.Equals(MongoType.DocumentDb.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Services.AddHangfire(config =>
            {
                var mongoUrlBuilder = new MongoUrlBuilder(connectionString);
                var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
                var opt = new CosmosStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        BackupStrategy = new NoneMongoBackupStrategy(),
                        MigrationStrategy = new DropMongoMigrationStrategy(),
                    }
                };
                config.UseCosmosStorage(mongoClient, mongoUrlBuilder.DatabaseName, opt);
            });
        }

        context.Services.AddHangfireServer(opt => { opt.Queues = new[] { "background" }; });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.AddBackgroundWorkerAsync<UserRelationWorker>();
        InitRecurringJob(context.ServiceProvider);
        //StartOrleans(context.ServiceProvider);
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        //StopOrleans(context.ServiceProvider);
    }
    
    private static void InitRecurringJob(IServiceProvider serviceProvider)
    {
        var jobsService = serviceProvider.GetRequiredService<IInitJobsService>();
        jobsService.InitRecurringJob();
    }

    private static void StartOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async () => await client.Connect());
    }

    private static void StopOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }
}