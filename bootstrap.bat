@echo off

:: Clear previous run status
COLOR

:: set batch file directory as current
pushd "%~dp0"

set SHARPMAKE_EXECUTABLE=tmp\bin\debug\Sharpmake.Application\Sharpmake.Application.exe

call CompileSharpmake.bat Sharpmake.Application/Sharpmake.Application.csproj Debug AnyCPU
if %errorlevel% NEQ 0 goto error
set SHARPMAKE_MAIN="Sharpmake.Main.sharpmake.cs"
if not "%~1" == "" (
    set SHARPMAKE_MAIN="%~1"
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
