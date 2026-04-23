namespace Rig.TUnit.Messaging.Nats.Topology;

public interface INatsStreamConfig
{
    INatsStreamConfig WithSubjects(params string[] subjects);
    INatsStreamConfig WithMaxMessages(long maxMsgs);
    INatsStreamConfig WithRetentionPolicy(NatsRetentionPolicy policy);
}
