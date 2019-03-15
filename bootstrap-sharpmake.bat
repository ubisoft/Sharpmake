@echo off
setlocal

:: set batch file directory as current
pushd "%~dp0"

set SHARPMAKE_EXECUTABLE=bin\debug\Sharpmake.Application.exe

:: Try to use vswhere to find the latest Visual Studio installation
set VSWHERE_PATH=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe
if exist %VSWHERE_PATH% (
    for /f "usebackq tokens=1* delims=: " %%i in (`"%VSWHERE_PATH%" -latest -requires Microsoft.VisualStudio.Workload.NativeDesktop`) do (
         if /i "%%i"=="installationPath" set VSINSTALLATION=%%j
    )
)

if "%VSINSTALLATION%"=="" (
    :: Fallback on VS2015 path
    set "VSCOMNTOOLS=%VS140COMNTOOLS%"
) else (
    :: Use latest Visual Studio installation
    set "VSCOMNTOOLS=%VSINSTALLATION%\Common7\Tools\"
)

if "%VSCOMNTOOLS%" == "" (
    echo ERROR: Cannot determine the location of the VS Common Tools folder.
    goto error
)

call "%VSCOMNTOOLS%VsMSBuildCmd.bat"
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
