@echo off
:: Batch arguments:
:: %~1: Project/Solution to build (default: Sharpmake.sln)
:: %~2: Target(Normally should be Debug or Release) (default: Debug)
:: %~3: Platform(Normally should be "AnyCPU") (default: AnyCPU)
:: Using a specific platform identifier, i.e. win-x64, linux-x64, ios-x64, will publish a single file executable for that platform
:: %~4: Framework - When using a platform specifier other than AnyCPU, you need to also specify the target framework
:: if none are passed, defaults to building Sharpmake.sln in Debug|AnyCPU

setlocal enabledelayedexpansion

set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist %VSWHERE% (
    echo ERROR: Cannot determine the location of the vswhere command Common Tools folder.
    goto error
)

set VSMSBUILDCMD=
for /f "usebackq delims=" %%i in (`%VSWHERE% -latest -products * -property installationPath`) do (
  if exist "%%i\Common7\Tools\VsMSBuildCmd.bat" (
    set VSMSBUILDCMD="%%i\Common7\Tools\VsMSBuildCmd.bat"
  )
)

if not defined VSMSBUILDCMD (
    echo ERROR: Cannot determine the location of Common Tools folder.
    goto error
)

echo MSBuild batch path: !VSMSBUILDCMD!
call !VSMSBUILDCMD!
if %errorlevel% NEQ 0 goto error

set PROJSLN_FILE="%~dp0Sharpmake.sln"
if not "%~1" == "" (
    set PROJSLN_FILE=%1
)

set TARGET="Debug"
if not "%~2" == "" (
    set TARGET=%2
)

set PLATFORM="AnyCPU"
set FRAMEWORK=""
if not "%~3" == "" (
    if not "%~3" == "AnyCPU" (
        if "%~3" == "" (
            echo ERROR: When specifying a specific platform "%~3" you must also give a target framework (i.e. net5.0)
        )
        set PLATFORM=%3
        set FRAMEWORK=%4
    )
)

call :BuildSharpmakeDotnet %PROJSLN_FILE% %TARGET% %PLATFORM% %FRAMEWORK%

if %errorlevel% EQU 0 goto success

echo Compilation with dotnet failed, falling back to the old way using MSBuild

call :BuildSharpmakeMSBuild %PROJSLN_FILE% %TARGET% %PLATFORM%

if %errorlevel% NEQ 0 goto error

goto success

@REM -----------------------------------------------------------------------
:: Build Sharpmake with dotnet using specified arguments
:BuildSharpmakeDotnet
echo Compiling %~1 in "%~2|%~3"...

set DOTNET_BUILD_CMD=dotnet build "%~1" -nologo -v m -c "%~2"
if not "%~3" == "AnyCPU" (
    :: If target a specific platform use full platform build command
    set DOTNET_BUILD_CMD=dotnet publish "%~1" -nologo -v m -c "%~2" -r %~3 -f %~4
)
echo %DOTNET_BUILD_CMD%
%DOTNET_BUILD_CMD%
set ERROR_CODE=%errorlevel%
if %ERROR_CODE% NEQ 0 (
    echo ERROR: Failed to compile %~1 in "%~2|%~3".
    goto end
)
goto success

@REM -----------------------------------------------------------------------
:: Build Sharpmake with MSBuild using specified arguments
:BuildSharpmakeMSBuild
echo Compiling %~1 in "%~2|%~3"...

set MSBUILD_CMD=msbuild -clp:Summary -t:rebuild -restore "%~1" /nologo /verbosity:m /p:Configuration="%~2" /p:Platform="%~3" /maxcpucount /p:CL_MPCount=%NUMBER_OF_PROCESSORS%
echo %MSBUILD_CMD%
%MSBUILD_CMD%
set ERROR_CODE=%errorlevel%
if %ERROR_CODE% NEQ 0 (
    echo ERROR: Failed to compile %~1 in "%~2|%~3".
    goto end
)
goto success

@REM -----------------------------------------------------------------------
:success
set ERROR_CODE=0
goto end

@REM -----------------------------------------------------------------------
:error
set ERROR_CODE=1
goto end

@REM -----------------------------------------------------------------------
:end
exit /b %ERROR_CODE%
