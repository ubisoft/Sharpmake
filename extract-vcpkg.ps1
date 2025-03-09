param ([string] $workingDirectory)
try 
{
    Write-Host "--- Bootstrap for vcpkg ---"
    # change working folder if one was given
    if($workingDirectory -ne "")
    {
        Push-Location -Path $workingDirectory
        if (-not $?)
        {
            throw "error when changing working dir : $LASTEXITCODE"
        }
    }
    $currentDir = Get-Location
    Write-Host "working folder : $currentDir" 

    # clean zip dest folder if exists
    if (Test-Path extern\vcpkg) 
    {
        Remove-Item extern\vcpkg -Recurse -Force
    }
    #unzip vcpkg
    Write-Host "extracting extern\vcpkg.zip to extern\vcpkg"
    Expand-Archive extern\vcpkg.zip -DestinationPath extern
    #back to latest working folder
    if($workingDirectory -ne "")
    {
        Pop-Location
        $currentDir = Get-Location
        Write-Host "working folder : $currentDir" 
    }
}
catch 
{
    Write-Error $PSItem.Exception
    exit 1
}

