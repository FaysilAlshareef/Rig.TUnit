# Ordering assertions

This document will carry the provider capability matrix for
`OrderingAssert.PerKeyMonotonic` and associated messaging-topology assertions once
Phases 1–5 of Feature 007 have shipped.

The matrix is populated from Phase 6 task **T063** and is left intentionally empty
here so the doc file exists for cross-linking from the top-level README and the
per-provider pages from day one.

For now, see:

- `src/Rig.TUnit.Messaging/Assertions/OrderingAssert.cs` — current API
- `planning/messaging-topology-and-sessions/README.md` — design context
- `.dotnet-ai-kit/features/007-messaging-topology-sessions/spec.md` — feature spec

Once the matrix is filled in, it will read:

| Provider       | Per-key ordering primitive | Assertion support |
|----------------|----------------------------|-------------------|
| ServiceBus     | `SessionId`                | _to be populated — T063_ |
| Kafka          | `Message.Key` + partition  | _to be populated — T063_ |
| SQS FIFO       | `MessageGroupId`           | _to be populated — T063_ |
| NATS JetStream | subject segment + ordered consumer | _to be populated — T063_ |
| RabbitMQ       | routing key + FIFO queue   | _to be populated — T063_ |
