<#
.SYNOPSIS
Runs the given executable with the given parameters

.DESCRIPTION
runs the executable passed as parameter, with the given arguments, in the given working folder. Wait for process to exit, and fail if exit code is not 0

.PARAMETER workingDirectory
specify the folder used as working folder for the executable. Omit if you want to use the current working folder.

.PARAMETER exeToRun
specify the path to the executable to run. 

.PARAMETER arguments
specify the arguments passed to the executable. Omit if the executable does not require arguments.

#>
param ([string] $exeToRun, [string] $arguments, [string] $workingDirectory)
try 
{
    $currentPath = Get-Location
    $Info = New-Object System.Diagnostics.ProcessStartInfo
    $Info.FileName = $exeToRun
    $Info.Arguments = $arguments
    if ($workingDirectory)
    {
        if ([System.IO.Path]::IsPathRooted($workingDirectory))
        {
            $Info.WorkingDirectory = $workingDirectory
        }
        else 
        {
            $Info.WorkingDirectory = Join-Path $currentPath $workingDirectory
        }
    }
    else 
    {
        $Info.WorkingDirectory = $currentPath
    }
    Write-Host "running $($Info.FileName) $($Info.arguments), working folder $($Info.WorkingDirectory)"
    $Process = New-Object System.Diagnostics.Process
    $Process.StartInfo = $Info
    $Process.Start()
    $Process.WaitForExit()
    Write-Host "process exit code : $($Process.ExitCode)"
    if ($Process.ExitCode -ne 0)
    {
        Write-Error "error detected, exit with code $($Process.ExitCode)"
        Exit $Process.ExitCode
    }
}
catch 
{
    Write-Error $PSItem.Exception
    exit 1
}
