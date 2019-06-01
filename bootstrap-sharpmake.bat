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

goto success

@REM -----------------------------------------------------------------------
:success
COLOR 2F
echo Bootstrap succeeded^!
timeout /t 5
exit /b 0

@REM -----------------------------------------------------------------------
:error
COLOR 4F
echo Bootstrap failed^!
pause
set ERROR_CODE=1
goto end

@REM -----------------------------------------------------------------------
:end
:: restore caller current directory
popd
exit /b %ERROR_CODE%
