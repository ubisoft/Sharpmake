# Samples

Sharpmake repository contains many samples that showcase various features. If you also consider the various CI systems used by Sharpmake, it can be tedious to add or modify a sample in these systems. To simplify this maintenance, sample jobs declarations are data driven from the file `SamplesDef.json` and are dynamically injected into CI pipelines. This file is also used by `RunSample.ps1` script to execute samples.

## Samples definition format

Here an example for the sample HelloWorld in `SamplesDef.json`:

```json
{
    "Name": "HelloWorld",
    "CIs": [ "github", "gitlab" ],
    "OSs": [ "windows-2019", "windows-2022" ],
    "Frameworks": [ "net6.0" ],
    "Configurations": [ "debug", "release" ],
    "TestFolder": "samples/HelloWorld",
    "Commands":
    [
        "./RunSharpmake.ps1 -workingDirectory {testFolder} -sharpmakeFile \"HelloWorld.sharpmake.cs\" -framework {framework}",
        "./Compile.ps1 -slnOrPrjFile \"helloworld_vs2019_win32.sln\" -configuration {configuration} -platform \"Win32\" -WorkingDirectory \"{testFolder}/projects\" -VsVersion {os} -compiler MsBuild",
        "&'./{testFolder}/projects/output/win32/{configuration}/helloWorld.exe'",
        "./Compile.ps1 -slnOrPrjFile \"helloworld_vs2019_win64.sln\" -configuration {configuration} -platform \"x64\" -WorkingDirectory \"{testFolder}/projects\" -VsVersion {os} -compiler MsBuild",
        "&'./{testFolder}/projects/output/win64/{configuration}/helloWorld.exe'"
    ]
}
```

Here the description for each properties:

- *Name*: Name of the sample.
- *CIs*: CI systems where the sample can be executed. Valid values: "github" and "gitlab". An empty array here will completely disable the sample on CI systems. gitlab is used internally at Ubisoft.
- *OSs*: Operating systems where can be executed. Valid values: "linux", "macos", "windows-2019" and "windows-2022".
- *Frameworks*: .NET frameworks used by Sharpmake executable. Currently only "net6.0" is supported.
- *Configuration*: Configurations that the sample support. Valid values: "debug" and "release".
- *TestFolder*: Base directory of the sample files.
- *Commands*: List of commands to execute for the sample. Note that these commands are executed with a Powershell Invoke-Expression cmdlet. So the command can be any valid Powershell expression. This also mean that they share the same context. Setting a variable in one command makes it available to subsequent commands.

## Adding a sample

If you need to add a new sample. Adding a new entry in `SamplesDef.json` should be the only thing you need to do. Once committed, CI systems should dynamically add a job for the new sample.
