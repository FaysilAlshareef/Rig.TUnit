# Rig.TUnit.Storage.FileSystem

In-process filesystem-backed storage provider using `System.IO.Abstractions`. No container required — creates a unique temp sandbox per fixture and cleans up on disposal. Ships `PathSandboxHelper` for traversal-safe path resolution.

## Install

```
dotnet add package Rig.TUnit.Storage.FileSystem
```

## Example

```csharp
await using var fs = new FileSystemFixture();
await fs.InitializeAsync();

var path = Path.Combine(fs.Root, "greeting.txt");
await File.WriteAllTextAsync(path, "hello");
```

### Fluent rig wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UseFileSystem(RigConnect.FromValue("/tmp/rig-fs"), cfg => { }));
```

### Path-traversal safety

```csharp
var resolved = PathSandboxHelper.Resolve(fs.Root, "sub/greeting.txt");
// UnauthorizedAccessException if relative path escapes the sandbox:
// PathSandboxHelper.Resolve(fs.Root, "../escape.txt");
```

## Options

`FileSystemFixtureOptions` — configured via `appsettings.json` under section `RigTUnit:FileSystem`:

- `RootPathPrefix` (default `"rigtunit-fs"`) — temp-dir prefix under `Path.GetTempPath()`.
- `CleanupOnDispose` (default `true`) — delete the sandbox root when the fixture disposes.

## Dependencies

`Rig.TUnit.Storage`, `System.IO.Abstractions`, `Microsoft.Extensions.Options`
