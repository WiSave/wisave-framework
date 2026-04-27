# WiSave Framework

Shared .NET building blocks for WiSave microservices.

## Packages

| Package | Purpose |
| --- | --- |
| `WiSave.Framework.Domain` | DDD aggregate root, stream identity, and domain exception primitives. |
| `WiSave.Framework.Application` | Application-layer command guard and aggregate repository abstraction. |
| `WiSave.Framework.EventSourcing` | Event type registry and event naming abstractions. |
| `WiSave.Framework.EventSourcing.Kurrent` | KurrentDB aggregate repository, persistent subscription adapter, and MassTransit forwarder. |

## Versioning

Packages use MinVer. All packable projects are released together with the same version.

- Stable packages are produced on `main` or `master` pushes. The publish workflow tags the merge commit first when it does not already have a `v*` tag, then MinVer uses that tag for all packages.
- Pull requests produce prerelease packages using a `preview.<pr>.<run>` suffix.
- NuGet package versions are immutable, so every pushed prerelease package includes a unique run suffix.

## Build

```bash
dotnet build WiSave.Framework.slnx
dotnet test WiSave.Framework.slnx
dotnet pack WiSave.Framework.slnx -c Release -o ./nupkg
```
