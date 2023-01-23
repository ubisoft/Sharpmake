<#
.SYNOPSIS
Runs the ci pipeline bootstrap.

.DESCRIPTION
Bootstrap script first compile Sharpmake.Application.csproj file with given configuration, then it runs sharpmake on given sharpmake cs file.

.PARAMETER sharpmakeFile
the .cs file passed as parameter when Sharpmake application is executed after it has been compiled.
default to 'Sharpmake.Main.sharpmake.cs'

.PARAMETER configuration
the configuration for which you want to compile Sharpmake. 
default to 'debug'

.PARAMETER framework
the framework on which sharpMake is built. 
default to 'net6.0'. 

.PARAMETER vsVersion
The version of visual studio toolchain you want to use. Can be :
- a visual studio number, like "2019" or "2022"
- a windows name (with syntax 'windows-xxxx'), like "windows-2019" or "windows-2022"
- the value "latest", to use latest available visual studio version available on the running machine
- omit, if you have already setup everything for visual build toolchain to run.
The purpose of this parameter is to search for the requested visual studio toolchain, and run appropriate visual studio batch to setup that toolchain.

.PARAMETER addMono
Linux only (ignored when on windows).
If specified, then Windows Sharpmake application is first executed through 'mono'. If it fails, then it fallbacks with no error on running sharpmake application for linux. 

#>
param ($sharpmakeFile='Sharpmake.Main.sharpmake.cs', $configuration='debug', $framework='net6.0', $vsVersion='')

Write-Host "--- Bootstrap ---"
# compile sharpmake exe
$sharpMakeAppPath = Join-Path 'Sharpmake.Application' 'Sharpmake.Application.csproj'
Write-Host "compile $sharpMakeAppPath"
./Compile.ps1 -slnOrPrjFile $sharpMakeAppPath -configuration $configuration -vsVersion $vsVersion -compiler "dotnet"
if(-Not $?) 
{
    throw "exit code : $LASTEXITCODE"
}

# run sharpmake exe on given file
./RunSharpmake.ps1 -sharpmakeFile $sharpmakeFile -configuration $configuration -framework $framework
if(-Not $?) 
{
    throw "exit code : $LASTEXITCODE"
}

Write-Host "--- End Bootstrap ---"
