using Microsoft.Extensions.Logging;

namespace YuqerejearniLearjiwhurhemcacemke
{
    public class F1
    {
        public F1(ILogger<F1> logger, Info info, F2 f2)
        {
            _logger = logger;
            Info = info;
            F2 = f2;
        }

        public Info Info { get; }

        public void Do()
        {
            _logger.LogInformation(Info.Id);
            F2.Do();
        }

        private readonly ILogger<F1> _logger;

        private F2 F2 { get; }
    }
}