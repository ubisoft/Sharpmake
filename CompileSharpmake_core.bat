@echo off
:: Batch arguments:
:: %~1: Project/Solution to build
:: %~2: Target(Normally should be Debug or Release)
:: %~3: (Optional) Platform to publish
:: Common platforms: win-x64, linux-x64, ios-x64

setlocal enabledelayedexpansion
: set batch file directory as current
pushd "%~dp0"

call :BuildSharpmake %1 %2 %3
goto end

:: Build Sharpmake using specified arguments
:BuildSharpmake

set TARGET=%~2
if "%TARGET%" EQU "" (
    set TARGET=Debug
)

set MSRESTORE_CMD=dotnet restore "%~1" -v q -nologo
set MSBUILD_CMD=dotnet build "%~1" -v q -nologo -c "%TARGET%"

:: PLATFORM messes up dotnet
set PLATFORM_ID=AnyCPU
if "%~3" NEQ "" (
    set PLATFORM_ID=%~3
    set MSBUILD_CMD=dotnet publish "%~1" -v q -nologo -c "%TARGET%" -r %~3 /p:PublishSingleFile=true
)

echo Compiling %~1 in "%TARGET%|%PLATFORM_ID%"...

echo %MSRESTORE_CMD%
%MSRESTORE_CMD%

echo %MSBUILD_CMD%
%MSBUILD_CMD%
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to compile %~1 in "%TARGET%|%PLATFORM_ID%".
    exit /b 1
)
exit /b 0

:: End of batch file
:end
popd

