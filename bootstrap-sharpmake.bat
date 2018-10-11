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

call :NugetRestore Sharpmake/Sharpmake.csproj
call :BuildCsproj Sharpmake.Application/Sharpmake.Application.csproj Debug AnyCPU

set SM_CMD=%SHARPMAKE_EXECUTABLE% /sources("Sharpmake.Main.sharpmake.cs") /verbose
echo %SM_CMD%
%SM_CMD%

echo Sharpmake solution generated.
goto end

:: NUGET RESTORE
:NugetRestore
echo Restoring nuget packages for %~1
nuget restore "%~1"
if errorlevel 1 (
    echo ERROR: Failed to restore nuget package for %~1
    goto error
)
exit /b 0

@REM -----------------------------------------------------------------------
:BuildCsproj
echo Compiling %~1 in "%~2|%~3"...

msbuild /nologo /verbosity:quiet /p:Configuration="%~2" /p:Platform="%~3" "%~1"
if errorlevel 1 (
    echo ERROR: Failed to compile %~1 in "%~2|%~3".
    goto error
)
exit /b 0

@REM -----------------------------------------------------------------------
:end
:: restore caller current directory
popd
COLOR
exit /b 0

@REM -----------------------------------------------------------------------
:error
:: restore caller current directory
popd
COLOR 4F
pause
exit /b 1
