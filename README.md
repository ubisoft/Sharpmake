# <img src="/docs/sharpmake_logo.svg" width="32" height="32"> Sharpmake

![build](https://github.com/ubisoft/Sharpmake/workflows/build/badge.svg)

## Introduction

Sharpmake is a generator for Visual Studio projects and solutions. It is similar to *CMake* and *Premake*, but it is designed for **speed** and **scale**. Sharpmake has been used at Ubisoft to generate several thousands of `.vcxproj`, `.csproj` and `.sln` files in a matter of seconds, and each of these projects can support a large number of Visual Studio configurations as well.

That makes Sharpmake ideal for the development of multi-platform games, where the number of platforms, the different levels of optimization, the multiple rendering APIs on PC and the level editor can quickly multiply the number of configurations a given code base must support. Sharpmake generates all those configurations at once, very quickly. Thus, it becomes trivial to generate and regenerate the entire project.

Sharpmake uses the C# language for its scripts, hence the name. That means you can edit your scripts in Visual Studio (or Visual Studio Code) and benefits from the default C# tooling (auto-completion, refactoring, debugger...).

Sharpmake can also generate makefiles and Xcode projects and can be run "natively" on any modern OSes that support recent version of the dotnet runtime.

Sharpmake was developed internally at Ubisoft for Assassin's Creed 3 in 2011. After experimenting with the other existing tools, it became clear that none of these solutions were performant enough to generate the number of configurations needed (at least not in a trivial way) and that a custom generator was needed.

## Documentation

The Sharpmake documentation is split in two places:
- the [wiki on GitHub](https://github.com/ubisoftinc/Sharpmake/wiki).
- the `doc` folder at the root of the project.

The Sharpmake source code also comes with samples that you can study.

## Building and running Sharpmake

Building and running Sharpmake is quite straightforward:
- Clone the Git repository
- Open the `Sharpmake.sln` solution located in the root folder
- Hit the run button (by default it will run the first sample)

## More Platforms

Sharpmake originally had support for game consoles, but Ubisoft pulled it out because those could not be open sourced. Sharpmake now has an extension system that allows support for these consoles to be added back at runtime.

More information about platforms can be found [here](doc/Platforms.md).

## Extending Sharpmake

Sharpmake is an open source project that come with some generic built-in features.

But as soon as we start speaking about additional features restricted by NDA (like for platforms), or for internal use only, it is handy to have a way to extend it.

The recommended solution is to follow this folder layout:
```
SharpmakeExtended:
 - 📁 Sharpmake
 - 📁 Sharpmake.Platforms
 - 📁 Sharpmake.Extensions
 -    Directory.build.props
 -    SharpmakeExtended.sln
```

1. `📁 Sharpmake`

The `Sharpmake` folder contains all the files of this Git repository.

We commonly call it Sharpmake *core*.

If you plan to version your *SharpmakeExtended* project under Git, you can use a *Git submodule* to pull on it directly.

2. `📁 Sharpmake.Platforms` (and `📁 Sharpmake.Extensions`)

*Platforms vs. Extensions*: there is no difference between them, these two folders are only used to tidy/split things a little.

These two locations are where you can add any additional platforms (or extensions) in their own dedicated folder:
```
📁 Sharpmake.Platforms
 - 📁 Sharpmake.Platform_A
      - *.cs
      - Sharpmake.Platform_A.csproj
 - 📁 Sharpmake.Platform_B
      - *.cs
      - Sharpmake.Platform_B.csproj
```

`Sharpmake.Application.csproj` (from Sharpmake *core*), automatically adds `.csproj` from these folders to its dependency list. This means they will automatically be built and copied to its output folder, and simply hitting the "Start Debugging" button will *just work*.

3. `Directory.build.props`

This file is used automatically by your `.csproj` from your platforms and extensions folders.

We recommend to - at least - import the same file from the Sharpmake *core* folder to re-use the same basic setup (target framework...). You can also customize/override any option after the import.

```xml
<Project>
  <!-- Rely on Sharpmake build setup -->
  <Import Project="Sharpmake/Directory.Build.props" />

  <!-- Add customization/override here -->
  <!-- ... -->
</Project>
```

4. `SharpmakeExtended.sln`

This solution is only to ease development for humans. It allows to have in a single IDE all the projects from both Sharpmake *core* and the extended ones.
