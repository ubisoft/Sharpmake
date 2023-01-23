<#
.SYNOPSIS
Runs the given executable in the given working directory

.DESCRIPTION
run the executable passed as parameter, in the given working folder

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
    # run given exe
    if ($workingDirectory)
    {
        Write-Host "running $exeToRun $arguments, working folder $workingDirectory"
        $p=Start-Process -PassThru -NoNewWindow -FilePath $exeToRun -ArgumentList $arguments -WorkingDirectory $workingDirectory
        Wait-Process -InputObject $p
    }
    else 
    {
        Write-Host "running $exeToRun $arguments"
        $p=Start-Process -PassThru -NoNewWindow -FilePath $exeToRun -ArgumentList $arguments
        Wait-Process -InputObject $p
    }
    [int] $exitCode = $p.ExitCode
    if($exitCode -ne 0) 
    {
        throw "exit code : $exitCode"
    }
}
catch 
{
    Write-Error $PSItem.Exception
    exit 1
}
