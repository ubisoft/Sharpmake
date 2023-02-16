<#
.SYNOPSIS
Runs Sharpmake.application with given parameters.

.DESCRIPTION
Runs Sharpmake.application (linux or windows), with given parameters. 
Sharpmake application is found in Sharpmake.Application/bin/$configuration/$framework folder.

.PARAMETER workingDirectory
specify the working folder to use when sharpmake is run. Omit if you want to use the current working folder.

.PARAMETER sharpmakeFile
the .cs file used by Sharpmake (default to Sharpmake.Main.sharpmake.cs)

.PARAMETER configuration
the configuration used to compile Sharpmake. Default to 'Release'. 
Used to find the folder from where sharpmake.application is run.

.PARAMETER framework
the framework used to compile Sharpmake. Default to 'net6.0'. 
Used to find the folder from where sharpmake.application is run.

.PARAMETER devenvVersion
if set, this parameter is passed as '/devenvversion' parameter to Sharpmake application. Omit if not required.

#>
param ([string] $workingDirectory="./", [string] $sharpmakeFile='Sharpmake.Main.sharpmake.cs', [string] $configuration='Release', [string] $framework='net6.0', [string] $devenvVersion)

try 
{
    Write-Host "run sharpmake.application on $sharpmakeFile"
    $curentDir = Get-Location

    $sharpmakeWinExe = Join-Path $curentDir 'Sharpmake.Application' 'bin' $configuration $framework 'Sharpmake.Application.exe'
    $sharpmakeLinuxExe = Join-Path $curentDir 'Sharpmake.Application' 'bin' $configuration $framework 'Sharpmake.Application'
    $arguments = "/sources('$sharpmakeFile') /verbose"
    if ($devenvVersion -ne "")
    {
        $arguments = "$arguments /devenvversion('$devenvVersion')"
    }

    if ($IsWindows)
    {
        # run on windows
        Write-Host "running on windows"
        Write-Host "$sharpmakeWinExe $arguments"
        Push-Location $workingDirectory
        Write-Host "working folder : $(Get-Location)" 
        & $sharpmakeWinExe $arguments
        Pop-Location 
        Write-Host "exit code : $LASTEXITCODE"
        if($LASTEXITCODE -and $LASTEXITCODE -ne 0) 
        {
            Write-Error "error $LASTEXITCODE during Sharpmake.Application execution"
            exit $LASTEXITCODE
        }
    }
    else 
    {
        #run on linux
        Write-Host "running on linux"
        Write-Host "$sharpmakeLinuxExe $arguments"
        chmod +x "$sharpmakeLinuxExe"
        Push-Location $workingDirectory
        Write-Host "working folder : $curentDir" 
        & $sharpmakeLinuxExe $arguments
        Pop-Location 
        Write-Host "exit code : $LASTEXITCODE"
        if($LASTEXITCODE -and $LASTEXITCODE -ne 0) 
        {
            Write-Error "error $LASTEXITCODE during Sharpmake.Application execution"
            exit $LASTEXITCODE
        }
    }
}
catch 
{
    Write-Error $PSItem.Exception
    exit 1
}

