namespace Company.Consumers
{
    using MassTransit;

    public class MassTransitConsumerConsumerDefinition :
        ConsumerDefinition<MassTransitConsumerConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<MassTransitConsumerConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 1000));
        }
    }
}