using Microsoft.Extensions.Logging;

namespace YuqerejearniLearjiwhurhemcacemke
{
    public class F2
    {
        public Info Info { get; }

        public F2(ILogger<F2> logger, Info info)
        {
            _logger = logger;
            Info = info;
        }

        private ILogger<F2> _logger;

        public void Do()
        {
            _logger.LogInformation(Info.Id);
        }
    }
}