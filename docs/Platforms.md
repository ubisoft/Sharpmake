# Platforms

## Open Platforms

Sharpmake ships with the platform extension *Sharpmake.CommonPlatforms.dll*. This is a .NET class library that contains everything you need to generate Visual Studio solutions for C++, C#, and C++/CLI code for Windows. Sharpmake also has support for generating Xcode and GNU Make-based projects for Mac and Linux.

## NDA Platforms

Sharpmake was originally designed with built-in support for generating projects and solutions for video game consoles, which are platforms under strict NDA and private SDKs.

In order to release Sharpmake to the open-source community, these platforms have been removed from the code base and an extension mechanism have been created to allow developers to add them back at runtime.

If you are an *authorized developer* outside of Ubisoft and would like to try Sharpmake to generate projects for Microsoft, Nintendo or Sony platforms, please contact one of the Sharpmake maintainers through the first party developer forums.

## Processor architecture and instruction set level

`Platform` says which operating system or console a target is for. Two other target fragments refine it:

- `ProcessorArchitecture`: `x86`, `x64` or `arm64`.
- `InstructionSetLevel`: `Baseline` or `APX`. A level is the instruction set tier an executable requires, so each level is built as its own executable.

Leaving a fragment at 0 means "the default of the platform", so existing definitions are unchanged: `Platform.win64` is x64 with the Baseline level. A mask (`ProcessorArchitecture.x64 | ProcessorArchitecture.arm64`, `InstructionSetLevel.Baseline | InstructionSetLevel.APX`) in `AddTargets` expands to one target per value. An expansion that contains an invalid combination is an error that names the `AddTargets` call; split it into several calls.

```csharp
    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Debug | Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.x64, InstructionSetLevel = InstructionSetLevel.Baseline | InstructionSetLevel.APX });
    AddTargets(new Target(Platform.win64, DevEnv.vs2026, Optimization.Debug | Optimization.Release) { ProcessorArchitecture = ProcessorArchitecture.arm64 });
```

See the `HelloWindowsArchitectures` sample.

### Valid combinations

An explicit architecture or level that the platform does not support is an error, reported when the configuration is built.

| Platform | Architectures (default first) | Levels |
|---|---|---|
| win32 | x86 | Baseline |
| win64 | x64, arm64 | x64: Baseline, APX. arm64: Baseline |
| linux | x64 | Baseline |
| Durango, Orbis, Scarlett, Prospero | x64 (fixed) | Baseline |
| Nx, Ounce | arm64 (fixed) | Baseline |
| mac, ios, android, ... | not declared yet: no explicit architecture | - |

The APX level needs:

- Visual Studio 2026 or newer.
- The `Default` platform toolset, or v145 or newer. With the LLVM or ClangCL toolsets, clang 22 or newer.
- MSVC 14.51 or newer, which has `/feature:APX`. This is not checked when generating, because the output must not depend on the machine that generates it.

The compiler switch (`/feature:APX`, or the clang equivalent) is always emitted for the APX level, and never for the Baseline level.

### Names

| Target | Simple string (paths) | Project configuration | Toolchain platform (.vcxproj) | Solution platform |
|---|---|---|---|---|
| win32 | `Win32` | `Release` | `Win32` | `Win32` |
| win64, x64 | `Win64` | `Release` | `x64` | `x64` |
| win64, x64 + APX | `Win64APX` | `Release APX` | `x64` | `x64-APX` |
| win64, arm64 | `Win64Armv8` | `Release` | `ARM64` | `ARM64` |

APX is not a Visual Studio platform, it builds as `x64`. It is told apart from the baseline x64 build by its configuration name in the project, and by its own platform in the solution. Generated folders are lowercase and never contain a space, for example `output\win64apx\release\`.

A custom `Solution.Configuration.PlatformName` is never modified by Sharpmake. Append `Util.GetArchitectureDisplaySuffix(target)` to it (it gives `""`, `x64-APX` or `ARM64`), otherwise the architectures of a solution share a platform name and are reported as duplicates.

### Adding the support to a platform

Override `DefaultProcessorArchitecture`, `SupportedProcessorArchitectures` and `GetSupportedInstructionSetLevels` in the platform descriptor. `GetDisambiguatedPlatformString`, `GetSolutionPlatformName` and `GetConfigurationNameSuffix` have working defaults. A descriptor that overrides nothing accepts no explicit architecture and only the Baseline level, so new combinations are rejected rather than silently generated wrong.

## Referencing platforms from scripts

Platforms are compiled as ordinary .NET class libraries that are referenced into Sharpmake script. Because scripts are standalone though, there is no `.csproj` to add that reference to. Instead, you specify a platform reference with the `Sharpmake.Reference` module attribute:

```csharp
    [module: Reference("<path-of-your-platform>.dll")]
```

Sharpmake will look for DLL files in the executable's directory, although a relative path from the executable should work.

Please note that *Sharpmake.CommonPlatforms.dll* is always referenced, so putting `[module: Reference("Sharpmake.CommonPlatforms.dll")]` in your scripts is redundant.

There is no problem referencing platforms directly from scripts. The platform system is designed to isolate Sharpmake itself from the platforms, not the scripts.
