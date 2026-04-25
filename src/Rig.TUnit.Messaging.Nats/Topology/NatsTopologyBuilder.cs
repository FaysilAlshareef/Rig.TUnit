using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Rig.TUnit.Messaging.Nats.Topology;

public sealed class NatsTopologyBuilder : INatsTopologyBuilder
{
    private readonly INatsJSContext _jetStream;
    private readonly List<StreamSpec> _streams = [];

    public NatsTopologyBuilder(INatsJSContext jetStream)
    {
        _jetStream = jetStream ?? throw new ArgumentNullException(nameof(jetStream));
    }

    public INatsTopologyBuilder Stream(string name, Action<INatsStreamConfig>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var cfg = new StreamConfigBuilder(name);
        configure?.Invoke(cfg);
        _streams.Add(new StreamSpec(cfg));
        return this;
    }

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        foreach (var spec in _streams)
        {
            var config = spec.Builder.Build();
            try
            {
                await _jetStream.CreateStreamAsync(config, ct).ConfigureAwait(false);
            }
            // 10058 = JSStreamNameExistErr (stream name already in use). Generic
            // 400 covers any "bad request" — including unrelated validation errors
            // such as invalid retention policy, which we do NOT want to mask by
            // falling back to UpdateStreamAsync.
            catch (NatsJSApiException ex) when (ex.Error.ErrCode == 10058)
            {
                await _jetStream.UpdateStreamAsync(config, ct).ConfigureAwait(false);
            }
        }
    }

    private sealed record StreamSpec(StreamConfigBuilder Builder);

    private sealed class StreamConfigBuilder : INatsStreamConfig
    {
        private readonly string _name;
        private readonly List<string> _subjects = [];
        private long _maxMsgs = -1;
        private StreamConfigRetention _retention = StreamConfigRetention.Limits;

        internal StreamConfigBuilder(string name) => _name = name;

        public INatsStreamConfig WithSubjects(params string[] subjects)
        {
            _subjects.AddRange(subjects);
            return this;
        }

        public INatsStreamConfig WithMaxMessages(long maxMsgs)
        {
            _maxMsgs = maxMsgs;
            return this;
        }

        public INatsStreamConfig WithRetentionPolicy(NatsRetentionPolicy policy)
        {
            _retention = policy switch
            {
                NatsRetentionPolicy.Interest => StreamConfigRetention.Interest,
                NatsRetentionPolicy.WorkQueue => StreamConfigRetention.Workqueue,
                _ => StreamConfigRetention.Limits
            };
            return this;
        }

        internal StreamConfig Build() =>
            new(_name, _subjects)
            {
                Retention = _retention,
                MaxMsgs = _maxMsgs
            };
    }
}
