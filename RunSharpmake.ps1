<#
.SYNOPSIS
Runs Sharpmake.application with given parameters.

.DESCRIPTION
Runs Sharpmake.application (linux or windows), with given parameters. 
Sharpmake application is found in tmp/bin/$configuration/$framework folder.

.PARAMETER workingDirectory
specify the working folder to use when sharpmake is run. Omit if you want to use the current working folder.

.PARAMETER sharpmakeFile
the .cs file used by Sharpmake (default to Sharpmake.Main.sharpmake.cs)

.PARAMETER configuration
the configuration used to compile Sharpmake. Default to 'release'. 
Used to find the folder from where sharpmake.application is run.

.PARAMETER framework
the framework used to compile Sharpmake. Default to 'net6.0'. 
Used to find the folder from where sharpmake.application is run.

.PARAMETER devenvVersion
if set, this parameter is passed as '/devenvversion' parameter to Sharpmake application. Omit if not required.

.PARAMETER addMono
Linux only (ignored when on windows).
If specified, then Windows Sharpmake application is first executed through 'mono'. If it fails, then it fallbacks with no error on running sharpmake application for linux. 

#>
param ([string] $workingDirectory="./", [string] $sharpmakeFile='Sharpmake.Main.sharpmake.cs', [string] $configuration='release', [string] $framework='net6.0', [string] $devenvVersion, [switch] $addMono)

try 
{
    Write-Host "run sharpmake.application on $sharpmakeFile"
    $curentDir = Get-Location
    Write-Host "working folder : $curentDir" 

    $sharpmakeWinExe = Join-Path 'tmp' 'bin' $configuration $framework 'Sharpmake.Application.exe'
    $sharpmakeLinuxExe = Join-Path 'tmp' 'bin' $configuration $framework 'Sharpmake.Application'
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
        $p=Start-Process -passthru -NoNewWindow -FilePath $sharpmakeWinExe -ArgumentList $arguments -WorkingDirectory $workingDirectory
        # need to query the handle so that the process gets porperly initialized
        $handle = $p.Handle
        $p.WaitForExit()
        [int] $exitCode = $p.ExitCode
        if($exitCode -ne 0) 
        {
            throw "error $exitCode during Sharpmake.Application execution"
        }
    }
    else 
    {
        #run on linux
        Write-Host "running on linux"
        if ($addMono)
        {
            #run windows exe with mono
            $monoArgs = "--debug $sharpmakeWinExe $arguments"
            Write-Host "mono $sharpmakeWinExe $monoArgs"
            $p=Start-Process -passthru -NoNewWindow -FilePath "mono" -ArgumentList $monoArgs -WorkingDirectory $workingDirectory
            # need to query the handle so that the process gets porperly initialized
            $handle = $p.Handle
            $p.WaitForExit()
            [int] $exitCode = $p.ExitCode
            if($exitCode -ne 0) 
            {
                Write-Host "error $exitCode during mono Sharpmake.Application.exe execution -- ignored"
            }
            else 
            {
                Write-Host "success"
                return
            }
        }
    
        # run linux exe
        Write-Host "$sharpmakeLinuxExe $arguments"
        chmod +x "./$sharpmakeLinuxExe"
        $p=Start-Process -passthru -NoNewWindow -FilePath "./$sharpmakeLinuxExe" -ArgumentList $arguments -WorkingDirectory $workingDirectory 
        # need to query the handle so that the process gets porperly initialized
        $handle = $p.Handle
        $p.WaitForExit()
        [int] $exitCode = $p.ExitCode
        if($exitCode -ne 0) 
        {
            throw "error $exitCode during Sharpmake.Application execution"
        }
    }
}
catch 
{
    Write-Error $PSItem.Exception
    exit 1
}

