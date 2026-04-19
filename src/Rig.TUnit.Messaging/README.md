# Rig.TUnit.Messaging

Messaging base layer. Ships `IMessagingRig`, `MessagingFixtureBase`, `MessagingRigBuilder<TSelf>`, `ListenerBase<T>`, `EventSenderBase` (correlation/causation/W3C traceparent), `MessageAssert`, `DeadLetterAssert`, `OrderingAssert`, `TopicNamingConvention`. Concrete providers: `.ServiceBus`, `.Kafka`, `.RabbitMq`, `.Sqs`, `.Nats`.

## Install

```
dotnet add package Rig.TUnit.Messaging.ServiceBus
```

## Dependencies

`Rig.TUnit.Core`
