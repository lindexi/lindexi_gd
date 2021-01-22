using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;

namespace NurhearkujaynearJurjerewhairral
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<CLogger>();
            services.AddLogging(builder => builder.Services.AddSingleton<ILoggerProvider, FLoggerProvider>());

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class CLogger : ILogger
    {
        private readonly HttpContext _context;
        private readonly ILogger<CLogger> _logger;

        public CLogger(ILogger<CLogger> logger)
        {
            _logger = logger;
            TraceId = Guid.NewGuid().ToString("N");
        }

        public string TraceId { get; set; }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }
    }

    class F : ILoggerFactory
    {
        public F()
        {
        }

        public void Dispose()
        {

        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }
    }


    class FLoggerProvider : ILoggerProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FLoggerProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                Console.WriteLine(_httpContextAccessor.HttpContext.TraceIdentifier);
            }

            return new CLogger2(_httpContextAccessor);
        }

        public class CLogger2 : ILogger
        {
            private readonly IHttpContextAccessor _httpContextAccessor;

            public CLogger2(IHttpContextAccessor httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
                TraceId = Guid.NewGuid().ToString("N");
            }

            public string TraceId { get; set; }

            public IDisposable BeginScope<TState>(TState state)
            {
                return new F1();
            }

            class F1 : IDisposable
            {
                public void Dispose()
                {
                }
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var message = formatter(state, exception);
                Console.WriteLine($"CLogger={_httpContextAccessor.HttpContext?.TraceIdentifier} {message} {message.Contains(_httpContextAccessor.HttpContext?.TraceIdentifier ?? "")} {Thread.GetCurrentProcessorId()}");
            }
        }
    }
}
