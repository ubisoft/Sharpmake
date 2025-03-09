<#
.SYNOPSIS
Compile a project on various platforms.

.DESCRIPTION
Compile the given project file with the given configuration, on the given platform, using the given compiler

.PARAMETER workingDirectory
specify the working folder where compilation will talke place.
This is the folder where the project file is found.

.PARAMETER slnOrPrjFile
the project fileName whatever it is (sln, make, xcworkspace).
Only provide the project filename here, path to this file must be set as the working folder.

.PARAMETER configuration
the configuration used to compile your project. Default to 'debug'.

.PARAMETER platform
the platform used to compile your project (Any CPU, x64, win32,...)

.PARAMETER vsVersion
The version of visual studio toolchain you want to use for your build. Can be :
- a visual studio number, like "2019" or "2022"
- a windows name (with syntax 'windows-xxxx'), like "windows-2019" or "windows-2022"
- the value "latest", to use latest available visual studio version available on the running machine
- omit, if you have already setup everything for visual build toolchain to run.
The purpose of this parameter is to search for the requested visual studio toolchain, and run appropriate visual studio batch to setup that toolchain.

.PARAMETER compiler
The compiler you want to use for your build. available values are "dotnet", "xcode", "make", "msbuild"

.PARAMETER scheme
The 'scheme' parameter required when the xcodebuild compiler is selected. Ignored for all other compilers.
#>
param ([string] $slnOrPrjFile, [string] $configuration='debug', [string] $platform, [string] $vsVersion, [string] $compiler, [string] $workingDirectory, [string] $scheme)

function Get-MsBuildCmd
{
    param ([int]$VsVersion)

    #find 'where' tool
    $VsWhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio' 'Installer' 'vswhere.exe'
    if (-not(Test-Path -Path $VsWhere -PathType Leaf))
    {
        throw "could not find '$VsWhere' file on local machine"
    }

    if ($vsVersion -eq 0)
    {
        # latest available visual version requested
        $vsVersions = &"$VsWhere" -latest -products * -property installationPath
        $vsMsBuildCmd = Join-Path $vsVersions 'Common7' 'Tools'
        if (Test-Path -Path $vsMsBuildCmd -PathType Container)
        {
            #found !
            Write-Host "msbuild batch path : $vsMsBuildCmd"
            return $vsMsBuildCmd
        }
    }
    else
    {
        # specific visual version required
        $vsVersions = &"$VsWhere" -products * -property installationPath
        # scan all available versions, find the first one with visual version number in the path
        foreach ($vsVersionsItem in $vsVersions)
        {
            $vsMsBuildCmd = Join-Path $vsVersionsItem 'Common7' 'Tools'
            if (Test-Path -Path $vsMsBuildCmd -PathType Container )
            {
                if ($vsMsBuildCmd -like "*$VsVersion*")
                {
                    # found !
                    Write-Host "msbuild batch path : $vsMsBuildCmd"
                    return $vsMsBuildCmd
                }
            }
        }
    }
}

try
{
    $runProcessPath = Join-Path "$(Get-Location)" "RunProcess.ps1"
    Write-Host "--- Compile $slnOrPrjFile, $configuration|$platform with $vsVersion $compiler"
    # change working folder if one was given
    if($workingDirectory -ne "")
    {
        Push-Location -Path $workingDirectory
        if (-not $?)
        {
            throw "error when changing working dir : $LASTEXITCODE"
        }

    }
    Write-Host "working folder : $(Get-Location)"

    # if a VsVersion was requested, find and run matching VsMsBuildCommand, else just run the build that is supposed to be in the path already
    if($vsVersion -ne "")
    {
        # by default, get latest available vs version
        [int]$vsVersionNumber = 0
        if ($vsVersion -ne "latest")
        {
            # a specific version is required
            # remove potential "windows-" prefix on given vsVersion
            $vsVersion = $vsVersion -replace "windows-"
            $vsVersionNumber = [int]$vsVersion
        }
        # setup Build toolchain for given visual studio version
        Write-Host "setup toolchain"
        # find requested visual studio version
        $msBuildCommand = Get-MsBuildCmd -VsVersion $vsVersionNumber
        if ($msBuildCommand -eq "")
        {
            # not found
            throw "could not find visual build toolchain $vsVersion"
        }
        # set msbuild batch path as current working folder
        Push-Location $msBuildCommand
        if (-not $?)
        {
            throw "error when changing working dir : $LASTEXITCODE"
        }

        # run visual studio setup batch and gather all changed environment variables. this is required because batch is run in another process,
        # so we must gather result environment variable to update the ones in powershell process
        $modifiedEnvVars = cmd /c "VsMSBuildCmd.bat &set"
        # set powershell process env vars from the one returned by visual studio setup batch
        foreach ($line in $modifiedEnvVars)
        {
            If ($line -match "=")
            {
                $v = $line.split("=")
                Set-Item -Force -Path "ENV:\$($v[0])" -Value "$($v[1])"
            }
        }
        # back to previous working folder
        Pop-Location
    }

    Write-Host "working folder : $(Get-Location)"
    $binLogName = "msbuild_$configuration.binlog"
    if ($compiler -eq "dotnet")
    {
        if ($IsWindows)
        {
            Write-Host "dotnet compile on Windows"
            #dotnet compile
            $msBuildLog = Join-Path 'tmp' 'msbuild' 'windows' $binLogName
            dotnet build `"$slnOrPrjFile`" -nologo -v m -c `"$configuration`" -bl:`"$msBuildLog`"
            if($LASTEXITCODE -ne 0)
            {
                throw "error $LASTEXITCODE during dotnet compile"
            }
            Write-Host "compile success"
        }
        else
        {
            Write-Host "dotnet compile on Linux"
            #dotnet compile
            $osPath = $ImageOS + $platform
            $msBuildLog = Join-Path 'tmp' 'msbuild' $osPath $binLogName
            dotnet build -nologo -v m -bl:`"$msBuildLog`" -p:UseAppHost=true /p:_EnableMacOSCodeSign=false `"$slnOrPrjFile`" --configuration `"$configuration`"
            if($LASTEXITCODE -ne 0)
            {
                throw "error $LASTEXITCODE during dotnet compile"
            }
            Write-Host "compile success"
        }
    }
    elseif ($compiler -eq "xcode")
    {
        #xcode compile
        Write-Host "xcode compile"
        Write-Host $scheme
        # directly call xcodebuild, don't use start-process
        xcodebuild -workspace "$slnOrPrjFile" -configuration "$configuration" -scheme "$scheme"
        if($LASTEXITCODE -ne 0)
        {
            throw "error $LASTEXITCODE during xcode compile"
        }
        Write-Host "compile success"
    }
    elseif ($compiler -eq "make")
    {
        #make compile
        Write-Host "make compile"
        make -f "$slnOrPrjFile" config="$configuration"
        Write-Host "exit code : $LASTEXITCODE"
        if($LASTEXITCODE -and $LASTEXITCODE -ne 0)
        {
            Write-Error "error $LASTEXITCODE during make execution"
            exit $LASTEXITCODE
        }
        Write-Host "compile success"
    }
    else
    {
        #msbuild compile
        Write-Host "msbuild compile"
        $msBuildLog = Join-Path 'tmp' 'msbuild' 'windows' $binLogName
        msbuild $slnOrPrjFile -bl:"$msBuildLog" -clp:Summary -t:rebuild -restore /nologo /verbosity:m /p:Configuration="$configuration" /p:Platform="$platform" /maxcpucount /p:CL_MPCount=$env:NUMBER_OF_PROCESSORS
        Write-Host "exit code : $LASTEXITCODE"
        if($LASTEXITCODE -and $LASTEXITCODE -ne 0)
        {
            Write-Error "error $LASTEXITCODE during msbuild compile"
            exit $LASTEXITCODE
        }
        Write-Host "compile success"
    }
}
catch
{
    Write-Error $PSItem.Exception
    exit 1
}
finally
{
    # back to previous working folder, if one was pushed
    if($workingDirectory -ne "")
    {
        Pop-Location
        Write-Host "working folder : $(Get-Location)"
    }
}
