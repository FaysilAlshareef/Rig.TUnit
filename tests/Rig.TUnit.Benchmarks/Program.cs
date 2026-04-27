using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Rig.TUnit.Benchmarks;

// Merge our CI-tuned warmup/iteration counts on top of BDN's defaults
// (loggers, exporters, columns, validators). The workflow no longer passes
// --job on the CLI; this config is the single source of truth for run shape.
var config = ManualConfig
    .Create(DefaultConfig.Instance)
    .AddJob(new CiBenchmarkConfig().GetJobs().First());

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

public partial class Program { }
