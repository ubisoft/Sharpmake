## Introduction

Rider Json has project files of 2 levels:
+ ```root.json``` - "solution"-wide file which contains information about all projects
and their configurations;
+ ```<project>_<platform>_<configuration>.json``` - contains information about single configuration of the project.

The following is the mapping of Sharpmake properties to Rider Json.

## Relations between Sharpmake and Rider Json properties
### General
| Sharpmake | Rider Json |
| --------- | ---------- |
| ``Project.Configuration.ProjectName`` | ``Name`` |
| ``Project.Configuration.Name`` | ``Configuration`` |
| ``Configuration.Platform`` | ``Platform`` |
| ``IPlatformVcxproj.GetPlatformIncludePaths()`` | ``EnvironmentIncludePaths`` |
| ``Project.Configuration.Defines`` | ``EnvironmentDefinitions`` |
| ``Project.Configuration.ResolvedDependencies`` | ``Modules`` |

### Toolchain Info
| Sharpmake | Rider Json |
| --------- | ---------- |
| ``Options.<DevEnv>.Compiler.CppLanguageStandard``| ``ToolchainInfo.CppStandart``   |
| ``Options.<DevEnv>.Compiler.RTTI`` | ``ToolchainInfo.RTTI`` |
| ``Options.<DevEnv>.Compiler.Exceptions`` | ``ToolchainInfo.bUseExceptions`` |
| ``Project.Configuration.OutputType`` | ``ToolchainInfo.bIsBuildingLibrary`` <br/> ``ToolchainInfo.bIsBuildingDll``|
| ``Options.<DevEnv>.Compiler.Optimization`` | ``ToolchainInfo.bOptimizeCode`` |
| ``Options.Vc.Compiler.Inline`` | ``ToolchainInfo.bUseInlining`` |
| ``Project.Configuration.IsBlobbed`` <br/> ``Project.Configuration.FastBuildBlobbed`` | ``ToolchainInfo.bUseUnity`` |
| ``Options.Vc.General.DebugInformation`` <br/> ``Options.<Makefile / Clang>.Compiler.GenerateDebugInformation`` <br/> ``Options.XCode.Compiler.GenerateDebuggingSymbols`` <br/> ``Options.Android.Compiler.DebugInformationFormat`` | ``ToolchainInfo.bCreateDebugInfo`` |
| ``Options.Vc.Compiler.EnhancedInstructionSet`` | ``ToolchainInfo.bUseAVX`` |
| ``Configuration.Compiler`` | ``ToolchainInfo.Compiler`` |
| ``Options.Vc.Compiler.ConformanceMode`` | ``ToolchainInfo.bStrictConformanceMode`` |
| ``Options.Vc.SourceFile.PrecompiledHeader`` <br/> ``IPlatformVcxproj.HasPrecomp()`` | ``ToolchainInfo.PrecompiledHeaderAction`` |
| ``FastBuildMakeCommandGenerator.GetCommand()`` | ``ToolchainInfo.BuildCmd`` <br/> ``ToolchainInfo.ReBuildCmd`` <br/> ``ToolchainInfo.CleanCmd`` |

### Modules info
| Sharpmake | Rider Json |
| --------- | ---------- |
| ``Project.Configuration.ResolvedPublicDependencies`` | ``PublicDependencyModules`` |
| ``Project.Configuration.IncludePaths`` | ``PublicIncludePaths`` |
| ``Project.Configuration.IncludePrivatePaths`` | ``PrivateIncludePaths`` |
| ``Project.Configuration.ExportDefines`` | ``PublicDefinitions`` |
| ``Project.Configuration.Defines`` | ``PrivateDefinitions`` |


