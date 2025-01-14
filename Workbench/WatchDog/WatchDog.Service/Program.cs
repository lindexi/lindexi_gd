using dotnetCampus.Configurations;
using dotnetCampus.Configurations.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;
using StackExchange.Redis.Extensions.Newtonsoft;
using WatchDog.Service.Controllers;
using WatchDog.Service.Frameworks;

namespace WatchDog.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.AddWatchDog();
        builder.WebHost.UseUrls("http://0.0.0.0:57725");

        builder.Services.AddHttpClient();
        builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSimpleConsole());

        var coinFile = @"C:\lindexi\Work\Server_dev.coin";
        var fileConfigurationRepo = ConfigurationFactory.FromFile(coinFile,RepoSyncingBehavior.Static);
        var appConfigurator = fileConfigurationRepo.CreateAppConfigurator();

        builder.Services.AddSingleton(t =>
        {
            var configuration = appConfigurator.Of<RedisCoinConfiguration>();
            var redisConfiguration = new RedisConfiguration()
            {
                Hosts = new RedisHost[]
                {
                    new RedisHost()
                    {
                        Host = configuration.Address,
                        Port = configuration.Port
                    },
                },
                ConnectionString = $"{configuration.Address}:{configuration.Port}",
                AbortOnConnectFail = false,
                ConfigurationOptions =
                {
                    AbortOnConnectFail = false,
                }
            };
            //redisConfiguration.ConfigurationOptions.EndPoints.Add(configuration.Address, configuration.Port);
            return redisConfiguration;
        });
        builder.Services.AddSingleton(t =>
        {
            var logger = t.GetRequiredService<ILogger<RedisConnectionPoolManager>>();
            var redisConfiguration = t.GetRequiredService<RedisConfiguration>();
            var redisConnectionPoolManager = new RedisConnectionPoolManager(redisConfiguration, logger);
            return redisConnectionPoolManager;
        });
        builder.Services.AddSingleton<IRedisClient>(t =>
        {
            var redisConnectionPoolManager = t.GetRequiredService<RedisConnectionPoolManager>();
            var redisConfiguration = t.GetRequiredService<RedisConfiguration>();
            IRedisClient redisClient = new RedisClient(redisConnectionPoolManager, new NewtonsoftSerializer(),
                redisConfiguration);
            return redisClient;
        });
    
        var app = builder.Build();
        // Configure the HTTP request pipeline.

        app.UseAuthorization();
        app.UseWatchDog();

        var client = app.Services.GetRequiredService<IRedisClient>();

        //Task.Run(async () =>
        //{
        //    await client.Db0.AddAsync("testSADFmnasoidjfoipasjdf", "asdf");
        //    var t = await client.Db0.GetAsync<string>("testSADFmnasoidjfoipasjdf");
        //});

        app.MapControllers();

        app.Run();
    }

    class RedisCoinConfiguration : Configuration
    {
        public RedisCoinConfiguration() : base("Redis")
        {
        }

        public string Address
        {
            get => GetString();
        }

        public int Port
        {
            get => GetInt32() ?? 6379;
        }
    }
}
