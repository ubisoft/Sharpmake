@echo off

:: set batch file directory as current
pushd "%~dp0"

set SHARPMAKE_EXECUTABLE=bin\debug\Sharpmake.Application.exe

call CompileSharpmake.bat Sharpmake.Application/Sharpmake.Application.csproj Debug AnyCPU
if %errorlevel% NEQ 0 goto error

set SM_CMD=%SHARPMAKE_EXECUTABLE% /sources("Sharpmake.Main.sharpmake.cs") /verbose
echo %SM_CMD%
%SM_CMD%
if %errorlevel% NEQ 0 goto error

call :NugetRestore Sharpmake.sln win
if %errorlevel% NEQ 0 goto error

goto success

:NugetRestore
echo Restoring nuget packages for %~1

set DOTNET_RESTORE=dotnet restore "%~1"
if "%~2" neq "" set DOTNET_RESTORE=%DOTNET_RESTORE% -r %2
echo %DOTNET_RESTORE%
%DOTNET_RESTORE%
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to restore nuget package for %~1
    exit /b 1
)
exit /b 0

@REM -----------------------------------------------------------------------
:success
COLOR 2F
echo Bootstrap succeeded^^!
timeout /t 5
exit /b 0

@REM -----------------------------------------------------------------------
:error
COLOR 4F
echo Bootstrap failed^^!
pause
set ERROR_CODE=1
goto end

@REM -----------------------------------------------------------------------
:end
:: restore caller current directory
popd
exit /b %ERROR_CODE%
