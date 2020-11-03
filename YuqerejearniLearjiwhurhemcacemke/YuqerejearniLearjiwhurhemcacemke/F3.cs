using Microsoft.Extensions.Logging;

namespace YuqerejearniLearjiwhurhemcacemke
{
    public class F3
    {
        public F3(ILogger<F3> logger, Info info)
        {
            _logger = logger;
            Info = info;
        }

        public Info Info { get; }

        public void Do()
        {
            _logger.LogInformation(Info.Id);
        }

        private readonly ILogger<F3> _logger;
    }
}