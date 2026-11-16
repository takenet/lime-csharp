# .NET 10 upgrade (FRO-607)

This document records the upgrade of `lime-csharp` to .NET 10 (`net10.0`): what changed, why, and the evidence collected while validating it.

- Scope: projects in `src/Lime.sln`.
- Strategy: **additive**. `net10.0` is added next to every existing library target, and no target is removed. Tools, samples, and test projects are retargeted to `net10.0` only.
- SDK used for validation: .NET SDK `10.0.404` (runtime `10.0.15`) on Linux x64.
- Baseline: `master` at `6e44e3be`, built with the same SDK. Baseline tests ran their `net6.0` binaries on runtime 8 through `DOTNET_ROLL_FORWARD=Major`, because no .NET 6 runtime was available.

## Target framework matrix

| Project | Before | After |
|---|---|---|
| `Lime.Protocol` | `netstandard2.0;netstandard2.1;net462` | `netstandard2.0;netstandard2.1;net462;net10.0` |
| `Lime.Messaging` | `netstandard2.0;net462` | `netstandard2.0;net462;net10.0` |
| `Lime.Transport.Redis` | `netstandard2.0;net462` | `netstandard2.0;net462;net10.0` |
| `Lime.Transport.Tcp` | `netstandard2.1` | `netstandard2.1;net10.0` |
| `Lime.Transport.WebSocket` | `netstandard2.1` | `netstandard2.1;net10.0` |
| `Lime.Transport.AspNetCore` | `net6.0` | `net6.0;net10.0` |
| `Lime.Cli`, `Lime.Benchmarks`, samples, `Lime.Protocol.ConsoleTests` | `net6.0` / `netcoreapp*` | `net10.0` |
| Test projects (`*.UnitTests`, `*.UnitTests.Common`, `Lime.Protocol.LoadTests`) | `net6.0` | `net10.0` |

`LangVersion` was raised to `latest` in the projects that pinned an older value, as the plan specifies.

## Code changes

1. **`src/Lime.Protocol/Network/EnvelopePipe.cs`, line 1.**
   - Change: `#if NETSTANDARD2_1` → `#if NETSTANDARD2_1 || NETCOREAPP`.
   - Why: without it, `EnvelopePipe` would not be compiled into the `net10.0` asset, and `PipeTcpTransport` / `PipeWebSocketTransport` could not build for `net10.0`.
   - Check: `lib/net10.0/Lime.Protocol.dll` and `lib/netstandard2.1/Lime.Protocol.dll` contain the type; `netstandard2.0` and `net462` do not, as before.
2. **`src/Lime.Protocol/Network/EnvelopePipe.cs`, resume writer threshold** (**not in the original plan**; separate commit).
   - Problem: on .NET 10, `PipeOptions` throws `ArgumentOutOfRangeException` (`resumeWriterThreshold`) when the implicit default resume threshold (32 KiB) is larger than an explicit pause threshold.
   - Effect: `PipeTcpTransportTests.SendAsync_BiggerThanPauseWriterThresholdMessageEnvelope_TransportShouldBeClosed` uses a 1 KiB pause threshold and failed after the retarget.
   - Fix: when the pause threshold is below 32 KiB, the resume threshold is capped to it. Otherwise it stays at the default (`-1`).
   - The default pause threshold (`DEFAULT_PAUSE_WRITER_THRESHOLD`, 8 MiB) is not affected.
3. **Compile fixes required by the newer packages** (**not in the original plan**):
   - `src/Lime.Sample.AspNetCore/Startup.cs`: `using Microsoft.OpenApi.Models;` → `using Microsoft.OpenApi;`. Swashbuckle 11 depends on Microsoft.OpenApi 2.x, which moved the models namespace.
   - `src/Lime.Transport.WebSocket.UnitTests/PipeWebSocketTransportTests.cs` and `WebSocketTransportTests.cs`: removed the unused `using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;`. That namespace is not in the ASP.NET Core 10 shared framework. No test code was changed or removed.
4. **`src/Lime.Protocol.ConsoleTests/Lime.Protocol.ConsoleTests.csproj`: `<IsTestingPlatformApplication>false</IsTestingPlatformApplication>`** (**not in the original plan**).
   - `NUnit3TestAdapter` 6.x brings in Microsoft.Testing.Platform through `buildTransitive`.
   - On this console project, that makes the .NET 10 SDK refuse the solution-wide `dotnet test` (VSTest) run.

`Lime.Transport.AspNetCore` needed no code changes: `WarningsAsErrors=nullable` builds cleanly against ASP.NET Core 10.

## Dependency changes

| Project | Change |
|---|---|
| `Lime.Protocol` | `Dawn.Guard`, `Newtonsoft.Json`, and `System.Threading.Tasks.Dataflow` stay unconditional. `System.Diagnostics.DiagnosticSource`, `System.Memory`, `System.Threading.Channels`, `System.Text.Json`, `System.Buffers`, and `System.IO.Pipelines` are now conditioned to `'$(TargetFramework)' != 'net10.0'` (they are inbox on .NET 10). |
| `Lime.Transport.Tcp` | `System.IO.Pipelines 4.6.0` is conditioned to `netstandard2.1`. `System.Net.NameResolution` and `System.Net.Security` were **removed** (deviation, see below). |
| `Lime.Transport.WebSocket` | Kestrel `2.3.0`, Kestrel.Core `2.3.6`, `Microsoft.AspNetCore.WebSockets 2.2.1`, and `System.Threading.Channels 4.7.0` are conditioned to `netstandard2.1`. `net10.0` uses `<FrameworkReference Include="Microsoft.AspNetCore.App" />`. `ReflectionMagic` is now 6.0.0. |
| `Lime.Cli` | `SimpleInjector` 5.6.0. |
| `Lime.Sample.Server` | `System.Buffers` removed (inbox). |
| `Lime.Sample.AspNetCore` | `Microsoft.AspNetCore.Mvc.NewtonsoftJson` 10.0.15, `Swashbuckle.AspNetCore` 11.0.1. |
| Test projects | `Microsoft.NET.Test.Sdk` 18.12.0, `NUnit3TestAdapter` 6.3.1, `NUnit` 3.14.0 (still NUnit 3), `Moq` 4.21.0, `Shouldly` 4.3.0. `TeamCity.VSTest.TestAdapter` removed. |

### Why `System.Net.NameResolution` / `System.Net.Security` were removed from `Lime.Transport.Tcp`

- Once a project targets `net10.0`, the .NET 10 SDK applies **package pruning** to all of its target frameworks.
- In the `netstandard2.1` graph of `Lime.Transport.Tcp`, both references were flagged with `NU1510`: "the package is part of the platform and will not be pruned".
- The plan requires a build with no `NU1510`.
- Both APIs are already part of `netstandard2.1`, so the references were redundant.
- The `netstandard2.1` dependency group of `Lime.Transport.Tcp.nupkg` now contains only `Lime.Protocol` and `System.IO.Pipelines 4.6.0`.

### Package pruning and the vulnerability audit

Package pruning also removes some *transitive* legacy packages from the restore graph of the older targets (`netstandard2.0`, `net6.0`) in projects that now also target `net10.0`. As a result, several advisories that the baseline audit reported for those targets **no longer show up in the post-upgrade audit**.

This is a masking effect of pruning, not a fix:

- The published `.nuspec` files declare the same dependencies as before for the legacy groups. For example, `Lime.Transport.Redis` still depends on `StackExchange.Redis 1.2.6` in every group.
- A consumer that restores the `netstandard2.0` / `net462` assets with an older SDK, or without pruning, still resolves the same vulnerable transitive versions.

These pre-existing findings remain **deferred** (see *Deferred work*).

## CI changes

- `azure-pipelines.yml`: added `dotnetVersion: 10.x` as the first parameter of the `template-pipeline.yml@templates` extension.
- All other parameters (`skipSast`, `csprojToPack`, etc.) are unchanged.
- Test execution in the pipeline (`skipTestToNuget`) was not enabled.
- `appveyor.yml` (legacy) was not touched.

## TLS policy

The `SslProtocols` defaults were **not changed**. The following files have no diff:

- `src/Lime.Transport.Tcp/TcpTransport.cs`: `Tls | Tls11 | Tls12`
- `src/Lime.Transport.Tcp/PipeTcpTransport.cs`: `Tls | Tls11 | Tls12 (| None)`
- `src/Lime.Transport.WebSocket/WebSocketTransportListener.cs`, `PipeWebSocketTransportListener.cs`: `Tls11 | Tls12`
- `src/Lime.Transport.WebSocket/Kestrel/KestrelWebSocketTransportListener.cs`: `None | Tls11 | Tls12`

On `net10.0`, these produce `SYSLIB0039` warnings because TLS 1.0/1.1 are obsolete. The warnings are accepted, not suppressed. At runtime, the operating system TLS stack may reject TLS 1.0/1.1 regardless of these flags.

## Validation evidence

### Restore / build / pack

| Step | Baseline (`master`) | After upgrade |
|---|---|---|
| `dotnet restore src/Lime.sln` | exit 0 | exit 0. No `NU1101`/`NU1102`/`NU1201`/`NU1510`. One `NU1902` (pre-existing advisory, see audit). |
| `dotnet build src/Lime.sln -c Release --no-restore` | exit 0, 0 errors, 269 warnings | exit 0, 0 errors, 426 warnings, 0 `NU1510` |
| `dotnet pack` (6 public packages, `--no-build`) | n/a | exit 0 for each package |

The plan's single `dotnet pack` command with six project paths fails with `MSB1008` (only one project can be specified), so each project was packed separately with the same options.

#### Warning breakdown

MSBuild prints each warning inline and again in the summary, so the counts below are doubled. The doubling is the same on both sides.

| Code | Baseline | After | Notes |
|---|---|---|---|
| CS1591 / CS1573 / CS1571 / CS1572 / CS1574 / CS1584 / CS1658 / CS1711 | 246 / 54 / 24 / 14 / 6 / 24 / 28 / 6 | 446 / 72 / 32 / 20 / 8 / 36 / 44 / 8 | XML doc warnings; higher only because more target frameworks are compiled |
| CS0618 | 70 | 122 | Same obsolete APIs, now also compiled for `net10.0` |
| NETSDK1138 | 12 | 0 | End-of-life `net6.0`-only projects were retargeted |
| SYSLIB0039 | 0 | 20 | **New**: TLS 1.0/1.1 flags (see *TLS policy*) |
| ASPDEPR008 / ASPDEPR004 | 0 | 8 / 4 | **New**: `WebHostBuilder` / `IWebHost` obsolete in ASP.NET Core 10 (`WebSocketTransportListener.cs`, `PipeWebSocketTransportListener.cs`) |
| SYSLIB0057 | 0 | 2 | **New**: `X509Certificate2` constructor in `Lime.Transport.Tcp.UnitTests/CertificateUtil.cs` |
| MSB3277 | 0 | 60 | **New**: version conflicts (`Microsoft.Extensions.Primitives`, `System.Diagnostics.DiagnosticSource`) in the `net6.0` build of `Lime.Transport.AspNetCore`, caused by pruning |
| MSB3245 / MSB3243 | 6 / 6 | 6 / 6 | Pre-existing (`net462` reference assemblies on Linux) |
| Others (CS0168, CS0108, CS0414, CS0105, SYSLIB0012) | 14 | 14 | Unchanged |

### Packages

| Package | `lib/` folders |
|---|---|
| `Lime.Protocol` | `net10.0`, `net462`, `netstandard2.0`, `netstandard2.1` |
| `Lime.Messaging` | `net10.0`, `net462`, `netstandard2.0` |
| `Lime.Transport.Redis` | `net10.0`, `net462`, `netstandard2.0` |
| `Lime.Transport.Tcp` | `net10.0`, `netstandard2.1` |
| `Lime.Transport.WebSocket` | `net10.0`, `netstandard2.1` |
| `Lime.Transport.AspNetCore` | `net10.0`, `net6.0` |

`.nuspec` checks:

- **`Lime.Transport.WebSocket`**
  - `net10.0` group: `Lime.Protocol`, `Newtonsoft.Json 13.0.3`, `ReflectionMagic 6.0.0`, and the framework reference `Microsoft.AspNetCore.App`. No Kestrel and no `Microsoft.AspNetCore.WebSockets`.
  - `netstandard2.1` group: still contains Kestrel `2.3.0`, Kestrel.Core `2.3.6`, and WebSockets `2.2.1`.
- **`Lime.Transport.Redis`**: `StackExchange.Redis 1.2.6` in the `net10.0`, `net462`, and `netstandard2.0` groups.
- **`Lime.Protocol`**: the `net10.0` group contains only `Dawn.Guard 1.12.0` and `Newtonsoft.Json 13.0.3`.

### Tests

Command: `dotnet test src/Lime.sln --configuration Release --filter "TestCategory!=Redis"`.
**Exit code: 1 both before and after the upgrade.**

| Project | Baseline: total / passed / failed / skipped | After: total / passed / failed / skipped |
|---|---|---|
| `Lime.Protocol.UnitTests` | 605 / 603 / 2 / 0 | 605 / 602 / 3 / 0 |
| `Lime.Transport.Tcp.UnitTests` | 108 / 108 / 0 / 0 | 108 / 108 / 0 / 0 |
| `Lime.Transport.WebSocket.UnitTests` | 70 / 66 / 4 / 0 (clean rerun; see below) | 70 / 66 / 4 / 0 |
| `Lime.Transport.AspNetCore.UnitTests` | 36 / 36 / 0 / 0 | 36 / 36 / 0 / 0 |
| `Lime.Transport.Redis.UnitTests` | 0 (no test matches the filter) | 0 (no test matches the filter) |
| `Lime.Protocol.LoadTests` | 41 / 41 / 0 / 0 | 41 / 41 / 0 / 0 |

How to read these results:

- **`Lime.Protocol.UnitTests`: flaky, pre-existing.**
  - The failing tests change from run to run. They are timing-dependent Moq verifications in `ResendMessagesChannelModuleTests` / `ChannelBaseTests`.
  - Running only those two fixtures (81 tests) three times per side gave 4, 1, and 1 failures after the upgrade, and 2, 1, and 2 failures on the baseline.
  - Tests seen failing on the baseline: `OnSending_MessageWithoutNotification_ShouldResendAfterInterval`, `OnStateChanged_PendingMessageAndRegisteredToNewChannel_SendsToNewChannel`, `OnSending_ReceivedNotificationAfterFirstResend_ShouldNotResendAgain`, `ChannelBaseTests.SendMessageAsync_TransportThrowsException_SendToTransportThenThrowException`, and `PingRemoteAsync_IdleChannel_SendsPing`.
- **`Lime.Transport.WebSocket.UnitTests`: 4 pre-existing failures, identical on both sides.**
  - Failing tests: `LocalEndPoint_ConnectedTransport_ShouldEqualsClientRemoteEndPoint` and `RemoteEndPoint_ConnectedTransport_ShouldEqualsClientLocalEndPoint`, in both `WebSocketTransportTests` and `PipeWebSocketTransportTests` (see the next section).
  - The first baseline run had 27 failures (70 / 43 / 27). Most of them came from the environment: `address already in use` on port 8081, another process was using the port. A clean baseline rerun gave 70 / 66 / 4 with the same 4 failures.
- **`Lime.Transport.Redis.UnitTests`: not exercised.**
  - Every test is `[Category("Redis")]`, so the filter selects nothing on either side.
  - Running `--filter TestCategory=Redis` gives 8/8 failures **on both sides** with `System.Exception: Redis is not running`.
  - Cause: `RedisFixture` detects Redis with `Process.GetProcessesByName("redis-server")`, which did not see the local `redis-server` in the validation sandbox. This is environmental and unrelated to the upgrade.
- **`Lime.Protocol.ConsoleTests`** is a console program with no tests; it produces an empty TRX.

No test was removed, and no `[Ignore]`, `[Explicit]`, or `Assert.Inconclusive` was added.

### WebSocket `LocalEndPoint` / `RemoteEndPoint` on `net10.0`

`ClientWebSocketTransport` (`ClientWebSocketTransport.cs:66,81`) and `PipeClientWebSocketTransport` (`PipeClientWebSocketTransport.cs:69,84`) read the endpoints through `ReflectionMagic.AsDynamic()` on private fields of `System.Net.WebSockets.ClientWebSocket`. On `net10.0` the getters **throw** instead of returning `null`. The same thing already happened on .NET 8 in the baseline.

```
System.MissingMemberException : The property _webSocket doesn't exist on type System.Net.WebSockets.WebSocketHandle.
Supported properties are: _abortSource, _negotiatedDeflateOptions, _state, HttpResponseHeaders, HttpStatusCode, State, WebSocket
```

As the plan requires, this behavior was **not changed**. Fixing it is deferred.

### Vulnerability audit

Command: `dotnet list src/Lime.sln package --vulnerable --include-transitive`.

Baseline (`master`; deduplicated; found across 15 projects):

| Package | Version | Severity | Advisory |
|---|---|---|---|
| Microsoft.AspNetCore.WebUtilities | 2.3.0 | Moderate | GHSA-6527-9fp8-r4x6 |
| System.Formats.Asn1 | 5.0.0 | High | GHSA-447r-wph3-92pm |
| System.Text.RegularExpressions | 4.3.0 | High | GHSA-cmhx-cq75-c4mj |
| System.Net.Http | 4.3.0 | High | GHSA-7jgj-8wvc-jh57 |
| System.Net.Security | 4.3.0 | High / Moderate | GHSA-6xh7-4v2w-36q6, GHSA-qhqf-ghgh-x2m4, GHSA-ch6p-4jcm-h8vh, GHSA-j8f4-2w4p-mhjc |
| Swashbuckle.AspNetCore.SwaggerUI | 5.6.3 | Moderate | GHSA-qrmm-w75w-3wpx |

After the upgrade, 18 projects report no vulnerable packages. One finding remains:

```
Project `Lime.Transport.WebSocket` has the following vulnerable packages
   [netstandard2.1]:
   Transitive Package                       Resolved   Severity   Advisory URL
   > Microsoft.AspNetCore.WebUtilities      2.3.0      Moderate   https://github.com/advisories/GHSA-6527-9fp8-r4x6

   [net10.0]: No vulnerable packages for this framework.
```

- **No advisory that was absent from the baseline was introduced.**
- The Swashbuckle finding was fixed by the bump to 11.0.1.
- The `System.*` 4.3.0 / `System.Formats.Asn1` findings:
  - in test, sample, and tool projects (now `net10.0` only): gone, because those projects no longer use the legacy packages.
  - in libraries that keep legacy targets: gone from the report only because of pruning (see *Package pruning and the vulnerability audit*). They are not fixed.

## Deviations from the approved plan

| Deviation | Reason |
|---|---|
| `EnvelopePipe.cs` changes beyond line 1 | Real .NET 10 regression in `PipeOptions` validation (see *Code changes*). Kept in a separate `fix(protocol)` commit so it can be reverted on its own. |
| `.cs` changes outside `EnvelopePipe.cs` / `Lime.Transport.AspNetCore` (`Startup.cs`, two WebSocket test files) | Compile errors with Swashbuckle 11 / ASP.NET Core 10. Only `using` directives changed. |
| `System.Net.NameResolution` / `System.Net.Security` removed from `Lime.Transport.Tcp` | Required to clear `NU1510`. |
| `IsTestingPlatformApplication=false` in `Lime.Protocol.ConsoleTests` | Required for the solution-wide `dotnet test` on the .NET 10 SDK. |
| Pack run once per project | The plan's multi-project `dotnet pack` fails with `MSB1008`. |
| `.csproj` files edited in place instead of fully rewritten | Keeps existing metadata (copyright, package info, encoding). |
| Baseline commit | The plan references `2203bf3`, which does not exist in the repository. The baseline is `master` at `6e44e3be`. |

## Deferred work

- Fix `LocalEndPoint` / `RemoteEndPoint` in the WebSocket client transports (reflection on private runtime fields has been broken since .NET 8).
- Move `WebSocketTransportListener` / `PipeWebSocketTransportListener` off `WebHostBuilder` / `IWebHost` (`ASPDEPR004` / `ASPDEPR008`).
- Review the TLS defaults (`SslProtocols`) and the `SYSLIB0039` / `CS0618` warnings.
- Upgrade `StackExchange.Redis` from 1.2.6 to 2.x (breaking for consumers).
- Migrate the tests to NUnit 4 (`ClassicAssert`).
- Fix the pre-existing vulnerable dependencies in the legacy targets, including `Microsoft.AspNetCore.WebUtilities 2.3.0` in `Lime.Transport.WebSocket` `netstandard2.1` and the findings masked by pruning.
- Stabilize the flaky `ResendMessagesChannelModuleTests` / `ChannelBaseTests` and make the Redis detection in `RedisFixture` reliable.
- Still out of scope: `appveyor.yml`, projects outside `src/Lime.sln`, `global.json` / `Directory.*.props`, and running tests in the Azure pipeline.
- Decide whether to drop the out-of-support `net6.0` target of `Lime.Transport.AspNetCore`, which would also remove the `MSB3277` warnings.

## Pending manual validation

- Run `Lime.Sample.Server` + `Lime.Sample.Client` on `net10.0` and confirm a LIME session over `net.tcp://localhost:55321`.
- Run the Azure DevOps PR pipeline and confirm that `UseDotNet@2` installs SDK 10.x and that the 6 `csprojToPack` projects are packed.
- Open `doc/lime-csharp-dotnet-10-upgrade-report.html` in a browser and check that its numbers match this document.
