using Microsoft.Extensions.Logging;

namespace YuqerejearniLearjiwhurhemcacemke
{
    public class F3
    {
        public Info Info { get; }

        public F3(ILogger<F3> logger, Info info)
        {
            _logger = logger;
            Info = info;
        }

        private ILogger<F3> _logger;

        public void Do()
        {
            _logger.LogInformation(Info.Id);
        }
    }
}