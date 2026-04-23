namespace Rig.TUnit.Messaging.RabbitMq.Topology;

public interface IRabbitMqExchangeConfig
{
    IRabbitMqExchangeConfig BindQueue(string queueName, string routingKey);
}
