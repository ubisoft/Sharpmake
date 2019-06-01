@echo off
:: Batch arguments:
:: %~1: Project/Solution to build
:: %~2: Target(Normally should be Debug or Release)
:: %~3: Platform(Normally should be "Any CPU" for sln and AnyCPU for a csproj)

setlocal enabledelayedexpansion
: set batch file directory as current
pushd "%~dp0"

set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist %VSWHERE% (
    echo ERROR: Cannot determine the location of the vswhere command Common Tools folder.
    goto error
)

set VSMSBUILDCMD=
for /f "usebackq delims=" %%i in (`%VSWHERE% -latest -property installationPath`) do (
  if exist "%%i\Common7\Tools\VsMSBuildCmd.bat" (
    set VSMSBUILDCMD="%%i\Common7\Tools\VsMSBuildCmd.bat"
  )
)

if not defined VSMSBUILDCMD (
    echo ERROR: Cannot determine the location of Common Tools folder.
    goto error
)
echo MSBuild batch path: !VSMSBUILDCMD!
call !VSMSBUILDCMD!
if %errorlevel% NEQ 0 goto end

call :BuildSharpmake %1 %2 %3
goto end

:: Build Sharpmake using specified arguments
:BuildSharpmake
echo Compiling %~1 in "%~2|%~3"...

set MSBUILD_CMD=msbuild -t:build -restore "%~1" /nologo /verbosity:quiet /p:Configuration="%~2" /p:Platform="%~3"
echo %MSBUILD_CMD%
%MSBUILD_CMD%
if %errorlevel% NEQ 0 (
    echo ERROR: Failed to compile %~1 in "%~2|%~3".
    exit /b 1
)
exit /b 0

:: End of batch file
:end
popd

