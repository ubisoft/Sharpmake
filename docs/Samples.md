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
- *OSs*: Operating systems where can be executed. Valid values: "linux", "macos", "windows-2019", "windows-2022" and "windows-2025-vs2026". Samples using "windows-2025-vs2026" must pass `-VsVersion latest` to `Compile.ps1`, since `{os}` can't be resolved to a Visual Studio version for that image.
- *Frameworks*: .NET frameworks used by Sharpmake executable. Currently only "net6.0" is supported.
- *Configuration*: Configurations that the sample support. Valid values: "debug" and "release".
- *TestFolder*: Base directory of the sample files.
- *Commands*: List of commands to execute for the sample. Note that these commands are executed with a Powershell Invoke-Expression cmdlet. So the command can be any valid Powershell expression. This also mean that they share the same context. Setting a variable in one command makes it available to subsequent commands.

## Adding a sample

If you need to add a new sample. Adding a new entry in `SamplesDef.json` should be the only thing you need to do. Once committed, CI systems should dynamically add a job for the new sample.

## Adding a regression test sample

Regression tests verify that Sharpmake's generated output does not change unexpectedly. Each test runs Sharpmake against a `.sharpmake.cs` file, writes output to a `projects/` subdirectory, and compares it byte-for-byte against a committed `reference/` directory.

### Directory layout

```
samples/
  <Category>/
    <SampleName>/
      <SampleName>.sharpmake.cs   # Sharpmake script
      codebase/                   # Source files for the sample project
        <ProjectName>/
          Program.cs
          ...
      reference/                  # Committed expected output (copy of projects/ after generation)
        ...
      projects/                   # Generated output (gitignored or transient, compared against reference/)
        ...
```

### Steps

1. **Create the sample directory** under `samples/` (or a subcategory like `samples/NetCore/`).

2. **Write the `.sharpmake.cs`** defining the project and solution. Set `conf.ProjectPath = @"[project.SourceRootPath]"` when you want the csproj to live alongside the source so the SDK can auto-discover files.

3. **Add source files** under `codebase/<ProjectName>/` that exercise the feature being tested.

4. **Generate the reference output.** From the sample directory, run:
   ```
   <path-to-Sharpmake.Application.exe> /sources(@'<Script>.sharpmake.cs') /outputdir(@'projects') /remaproot(@'.')
   ```
   The `/remaproot(@'.')` argument normalises absolute paths in the output to be relative, which is required for the reference comparison to work on any machine.

5. **Copy `projects/` to `reference/`** and commit the `reference/` directory.

6. **Add an entry to `regression_test.py`** in the `tests` list at the bottom of the file:
   ```python
   Test("NetCore\\MySample", "MySample.sharpmake.cs"),
   ```
   The path is relative to `samples/`.

7. **Add an entry to `SamplesDef.json`** so CI picks up the sample. Always include a `Compile.ps1` step, and for runnable output add a `RunProcess.ps1` step:
   ```json
   {
       "Name": "NetCore-MySample",
       "CIs": [ "github", "gitlab" ],
       "OSs": [ "windows-2022" ],
       "Frameworks": [ "net8.0" ],
       "Configurations": [ "debug", "release" ],
       "TestFolder": "samples/NetCore/MySample",
       "Commands":
       [
           "./RunSharpmake.ps1 -workingDirectory {testFolder} -sharpmakeFile \"MySample.sharpmake.cs\" -framework {framework}",
           "./Compile.ps1 -slnOrPrjFile \"MySampleSolution.vs2022.net8_0.sln\" -configuration {configuration} -platform \"Any CPU\" -WorkingDirectory \"{testFolder}/projects/MySample\" -VsVersion {os} -compiler MsBuild",
           "&'./{testFolder}/codebase/MySample/output/anycpu/{configuration}/net8.0/MySample.exe'"
       ]
   }
   ```
   The solution path depends on `conf.SolutionPath`; the output path depends on `conf.ProjectPath` and `OutputPath`. For net8.0 SDK-style projects, the SDK appends the TFM to the output directory (`net8.0/` subfolder). `DotNetConsoleApp` produces an apphost `.exe` shim alongside the `.dll`, so run the `.exe` directly.

8. **Add an entry to `UpdateSamplesOutput.bat`** alongside the other `NetCore\*` entries so the script can regenerate this sample's reference directory in one shot:
   ```bat
   call :UpdateRef samples NetCore\MySample  MySample.sharpmake.cs  reference  NetCore\MySample
   if not "%ERRORLEVEL_BACKUP%" == "0" goto error
   ```

### Regenerating the reference

If you intentionally change generated output (e.g. a new property or formatting fix), re-run `UpdateSamplesOutput.bat` (it regenerates all reference directories) or re-run step 4 for just your sample and replace `reference/` with the new output before committing.
