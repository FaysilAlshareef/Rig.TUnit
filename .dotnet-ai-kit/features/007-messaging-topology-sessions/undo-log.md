# Undo Log: 007-messaging-topology-sessions

## T000-RED — SendContext record shape + BuildHeaders overload parity
**Timestamp**: 2026-04-23T00:00:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Messaging.Tests.Unit/Helpers/SendContextTests.cs

## T000-GREEN — SendContext + BuildHeaders overload + CapturedMessage extension + listener null-coercion
**Timestamp**: 2026-04-23T00:00:00Z
**Repo**: primary
**Status**: OK

- created: src/Rig.TUnit.Messaging/Helpers/SendContext.cs
- modified: src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs (BuildHeaders overload added)
- modified: src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs (CapturedMessage<TMessage> SessionKey added, Body narrowed to string)
- modified: src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs (null-coercion on result.Message.Value)
- modified: src/Rig.TUnit.Messaging.Nats/Helpers/NatsListener.cs (null-coercion on msg.Data)
- modified: src/Rig.TUnit.Messaging.Sqs/Helpers/SqsListener.cs (null-coercion on msg.Body)
- modified: README.md (messaging section intro paragraph)

## T001-RED — ITopologyBuilder marker contract
**Timestamp**: 2026-04-23T00:00:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Messaging.Tests.Unit/Topology/ITopologyBuilderContractTests.cs

## T001-GREEN — ITopologyBuilder marker interface
**Timestamp**: 2026-04-23T00:00:00Z
**Repo**: primary
**Status**: OK

- created: src/Rig.TUnit.Messaging/Topology/ITopologyBuilder.cs
- created: docs/ordering-assertions.md (stub)

## T002 — regression guard against base-class WithTopology
**Timestamp**: 2026-04-23T00:00:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Messaging.Tests.Unit/Builder/MessagingRigBuilderNoGenericWithTopologyTests.cs
- modified: src/Rig.TUnit.Messaging/Builder/MessagingRigBuilder.cs (XML doc comment added)

## T003-RED — provider-parity driven by .parity-coverage.txt
**Timestamp**: 2026-04-23T00:00:00Z
**Repo**: primary
**Status**: OK

- modified: tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs (added 4 parity tests: ParityCoverageFile_Exists_WithLoadableAssemblies, Providers_InParityCoverage_DeclareWithTopology, Providers_InParityCoverage_DeclareSendContextOverload, SessionCapableProviders_InParityCoverage_DeclareSessionListener)
