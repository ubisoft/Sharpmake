@echo off
setlocal

:: set batch file directory as current
pushd "%~dp0"

set SHARPMAKE_EXECUTABLE=bin\debug\Sharpmake.Application.exe

if not defined VS140COMNTOOLS (
    echo ERROR: Cannot determine the location of the VS Common Tools folder.
    goto error
)

call "%VS140COMNTOOLS%VsMSBuildCmd.bat"
if %errorlevel% NEQ 0 goto error

call :NugetRestore Sharpmake/Sharpmake.csproj
if %errorlevel% NEQ 0 goto error

call :BuildCsproj Sharpmake.Application/Sharpmake.Application.csproj Debug AnyCPU
if %errorlevel% NEQ 0 goto error

set SM_CMD=%SHARPMAKE_EXECUTABLE% /sources("Sharpmake.Main.sharpmake.cs") /verbose
echo %SM_CMD%
%SM_CMD%
if %errorlevel% NEQ 0 goto error

goto success

@REM -----------------------------------------------------------------------
:NugetRestore
echo Restoring nuget packages for %~1
dotnet restore "%~1"
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to restore nuget package for %~1
    exit /b 1
)
exit /b 0

@REM -----------------------------------------------------------------------
:BuildCsproj
echo Compiling %~1 in "%~2|%~3"...

msbuild /nologo /verbosity:quiet /p:Configuration="%~2" /p:Platform="%~3" "%~1"
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to compile %~1 in "%~2|%~3".
    exit /b 1
)
exit /b 0

@REM -----------------------------------------------------------------------
:success
COLOR 2F
echo Boostrap succeeded!
timeout /t 5
exit /b 0

@REM -----------------------------------------------------------------------
:error
COLOR 4F
echo Boostrap failed!
pause
set ERROR_CODE=1
goto end

@REM -----------------------------------------------------------------------
:end
:: restore caller current directory
popd
exit /b %ERROR_CODE%
