using Microsoft.Extensions.Logging;

namespace YuqerejearniLearjiwhurhemcacemke
{
    public class F2
    {
        public F2(ILogger<F2> logger, Info info)
        {
            _logger = logger;
            Info = info;
        }

        public Info Info { get; }

        public void Do()
        {
            _logger.LogInformation(Info.Id);
        }

        private readonly ILogger<F2> _logger;
    }
}