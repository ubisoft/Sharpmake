@echo off
setlocal enabledelayedexpansion

:: set batch file directory as current
pushd "%~dp0"

set SHARPMAKE_EXECUTABLE=bin\debug\Sharpmake.Application.exe

set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist %VSWHERE% (
    echo ERROR: Cannot determine the location of the vswhere command Common Tools folder.
    goto error
)

set VSMSBUILDCMD=
for /f "usebackq delims=" %%i in (`%VSWHERE% -prerelease -latest -property installationPath`) do (
  if exist "%%i\Common7\Tools\VsMSBuildCmd.bat" (
    set VSMSBUILDCMD="%%i\Common7\Tools\VsMSBuildCmd.bat"
  )
)

if not defined VSMSBUILDCMD (
    echo ERROR: Cannot determine the location of Common Tools folder.
    goto error
)

call !VSMSBUILDCMD!
if %errorlevel% NEQ 0 goto error

call :NugetRestore Sharpmake/Sharpmake.csproj win
if %errorlevel% NEQ 0 goto error

call :BuildCsproj Sharpmake.Application/Sharpmake.Application.csproj Debug AnyCPU
if %errorlevel% NEQ 0 goto error

set SM_CMD=%SHARPMAKE_EXECUTABLE% /sources("Sharpmake.Main.sharpmake.cs") /verbose
echo %SM_CMD%
%SM_CMD%
if %errorlevel% NEQ 0 goto error

call :NugetRestore Sharpmake.sln win
if %errorlevel% NEQ 0 goto error

goto success

@REM -----------------------------------------------------------------------
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
:BuildCsproj
echo Compiling %~1 in "%~2|%~3"...

set MSBUILD_CMD=msbuild /nologo /verbosity:quiet /p:Configuration="%~2" /p:Platform="%~3" "%~1"
echo %MSBUILD_CMD%
%MSBUILD_CMD%
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to compile %~1 in "%~2|%~3".
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
