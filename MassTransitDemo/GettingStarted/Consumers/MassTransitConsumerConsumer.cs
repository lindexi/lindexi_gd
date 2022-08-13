using Microsoft.Extensions.Logging;

namespace Company.Consumers
{
    using System.Threading.Tasks;
    using MassTransit;
    using Contracts;

    public class MassTransitConsumerConsumer :
        IConsumer<MassTransitConsumer>
    {
        public MassTransitConsumerConsumer(ILogger<MassTransitConsumerConsumer> logger)
        {
            _logger = logger;
        }

        public Task Consume(ConsumeContext<MassTransitConsumer> context)
        {
            _logger.LogInformation($"{context.Message.Value}");
            return Task.CompletedTask;
        }

        private readonly ILogger _logger;

        
    }
}