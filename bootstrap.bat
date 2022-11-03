@echo off
:: Batch arguments:
:: %~1 Main sharpmake file
:: %~2 Target(Normally should be Debug or Release)
:: %~3 Framework (net5.0 or any configured framework in Sharpmake.Main.sharpmake.cs)
:: if none are passed, defaults to Sharpmake.Main.sharpmake.cs in Debug with net5.0

setlocal enabledelayedexpansion

:: Clear previous run status
COLOR

:: set batch file directory as current
pushd "%~dp0"

set SHARPMAKE_OPTIM=Debug
if not "%~2" == "" (
    set SHARPMAKE_OPTIM=%~2
)

set SHARPMAKE_FRAMEWORK=net5.0
if not "%~3" == "" (
    set SHARPMAKE_FRAMEWORK=%~3
)

set SHARPMAKE_EXECUTABLE=tmp\bin\%SHARPMAKE_OPTIM%\%SHARPMAKE_FRAMEWORK%\Sharpmake.Application.exe

call CompileSharpmake.bat Sharpmake.Application/Sharpmake.Application.csproj %SHARPMAKE_OPTIM% AnyCPU
if %errorlevel% NEQ 0 goto error
set SHARPMAKE_MAIN='Sharpmake.Main.sharpmake.cs'
if not "%~1" == "" (
    set SHARPMAKE_MAIN='%~1'
)

set SM_CMD=%SHARPMAKE_EXECUTABLE% /sources(%SHARPMAKE_MAIN%) /verbose
echo %SM_CMD%
%SM_CMD%
if %errorlevel% NEQ 0 goto error

goto success

@REM -----------------------------------------------------------------------
:success
COLOR 2F
echo Bootstrap succeeded^!
set ERROR_CODE=0
goto end

@REM -----------------------------------------------------------------------
:error
COLOR 4F
echo Bootstrap failed^!
set ERROR_CODE=1
goto end

@REM -----------------------------------------------------------------------
:end
:: restore caller current directory
popd
exit /b %ERROR_CODE%
