using Microsoft.Extensions.DependencyInjection;

namespace NallbiweeralCiherejajalne
{
    public class CommonStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }
    }
}
