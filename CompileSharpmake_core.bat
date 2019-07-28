@echo off
:: Batch arguments:
:: %~1: Project/Solution to build
:: %~2: Target(Normally should be Debug or Release)
:: %~3: Platform(Normally should be "Any CPU" for sln and AnyCPU for a csproj)

setlocal enabledelayedexpansion
: set batch file directory as current
pushd "%~dp0"

call :BuildSharpmake %1 %2 %3
goto end

:: Build Sharpmake using specified arguments
:BuildSharpmake
echo Compiling %~1 in "%~2|win-x64"...

set MSRESTORE_CMD=dotnet restore "%~1" -v q -nologo
echo %MSRESTORE_CMD%
%MSRESTORE_CMD%

set MSBUILD_CMD=dotnet build "%~1" -v q -nologo -c "%~2"

if "%~3" == "publish" (
  set MSBUILD_CMD=dotnet publish "%~1" -v q -nologo -c "%~2" -r win-x64 --self-contained true
)

echo %MSBUILD_CMD%
%MSBUILD_CMD%
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to compile %~1 in "%~2|win-x64".
    exit /b 1
)
exit /b 0

:: End of batch file
:end
popd

