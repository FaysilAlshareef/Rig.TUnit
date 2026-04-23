# Undo Log: 006-coverage-quality-uplift

## T030 - Grpc — 40.4 % → ≥ 90 %
**Timestamp**: 2026-04-22T00:00:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Grpc.Tests.Unit/TestInfrastructure/MinimalTestFactory.cs
- modified: tests/Rig.TUnit.Grpc.Tests.Unit/Helpers/GrpcClientHelperTests.cs (added GrpcClientHelper_CreateClient_ReturnsTypedChannel, GrpcClientHelper_Send_InvokesGrpcCall, GrpcClientHelper_SendAsync_InvokesGrpcCallAsync)
- modified: tests/Rig.TUnit.Grpc.Tests.Unit/Extensions/WebApplicationFactoryExtensionsTests.cs (added WebApplicationFactoryExtensions_CreateGrpcChannel_ReturnsWorkingChannel, WithTestConfiguration_WithConfigureServices_CallsConfigureServices, EndpointMappingStartupFilter_Configure_RegistersEndpoints)
- modified: tests/Rig.TUnit.Grpc.Tests.Unit/Builder/GrpcRigBuilderTests.cs (added UseGrpc_NullRigBuilder_ThrowsArgumentNullException, UseGrpc_NullFactory_ThrowsArgumentNullException, UseGrpc_NullConfigure_ThrowsArgumentNullException, UseGrpc_WithValidArgs_ReturnsSameRigBuilder)

## T033 - Messaging.ServiceBus — 59.7 % → ≥ 90 %
**Timestamp**: 2026-04-22T00:00:00Z
**Repo**: primary
**Status**: OK

- created: tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/ServiceBusListenerTests.cs (added ServiceBusEventSender_Send_DeliversMessageToQueue, ServiceBusEventSender_Send_WithProperties_SetsHeaders, ServiceBusListener_Ack_CompletesMessage, ServiceBusListener_Nack_AbandonsMessage, ServiceBusListener_DeadLetter_MovesMessageToDeadLetterQueue, ServiceBusListener_Retry_RedeliversAfterDelay)

## T060–T064 - Root README rewrite (all 14 sections)
**Timestamp**: 2026-04-22T00:00:00Z
**Repo**: primary
**Status**: OK

- modified: README.md (rewrote all 14 sections: headline+badges, What is Rig.TUnit, provider families table, quick-start, builder API, isolation, provider catalogue, running tests, benchmarks, CI pipeline, TDD discipline, contributing, architecture mermaid diagram, license)

## T090 - Remove continue-on-error from coverage gate
**Timestamp**: 2026-04-22T00:00:00Z
**Repo**: primary
**Status**: OK

- modified: .github/workflows/ci.yml (removed continue-on-error: true from coverage gate step; changed sys.exit(0) to sys.exit(1) in offenders branch; changed ::warning:: to ::error:: in offenders output)

## T065 - Add link-checker CI job
**Timestamp**: 2026-04-22T00:00:00Z
**Repo**: primary
**Status**: OK

- modified: .github/workflows/ci.yml (added linkcheck job using lycheeverse/lychee-action@v2 targeting README.md)

## T041 - Populate benchmarks/baseline-006.json
**Timestamp**: 2026-04-22T10:06:20Z
**Repo**: primary
**Status**: OK

- created: benchmarks/baseline-006.json (60 benchmark entries from 21 InProcessEmitBenchmarkConfig classes; all runtime fields contain .NET 10.0.5; capturedAt 2026-04-22T10:06:20Z)
- created: benchmarks/baseline-006-tmp/ (BDN artifacts directory — intermediate results, safe to delete)

## T042 - Update CI regression step to reference baseline-006.json
**Timestamp**: 2026-04-22T00:00:00Z
**Repo**: primary
**Status**: OK

- modified: .github/workflows/ci.yml (updated comment on line 640 from baseline-005.json to baseline-006.json; removed || echo "::warning::..." guard from benchmark run step)

## T043 - Add benchmark-action/github-action-benchmark@v1
**Timestamp**: 2026-04-22T00:00:00Z
**Repo**: primary
**Status**: OK

- modified: .github/workflows/ci.yml (added Store benchmark result step using benchmark-action/github-action-benchmark@v1 with tool=benchmarkdotnet, alert-threshold=120%, auto-push=true)
