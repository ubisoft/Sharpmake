@echo off
setlocal

:: set batch file directory as current
cd /d %~dp0%

set SHARPMAKE_EXECUTABLE=%~dp0%bin\debug\Sharpmake.Application.exe
set SHARPMAKEGEN_PATH=samples\SharpmakeGen

if "%VS140COMNTOOLS%"=="" (
    echo ERROR: Cannot determine the location of the VS Common Tools folder.
    goto error
    exit /b 1
)

@call "%VS140COMNTOOLS%VsMSBuildCmd.bat"

@call :BuildCsproj Sharpmake/Sharpmake.csproj Debug AnyCPU
@call :BuildCsproj Sharpmake.Generators/Sharpmake.Generators.csproj Debug AnyCPU
@call :BuildCsproj Sharpmake.Platforms/Sharpmake.CommonPlatforms/Sharpmake.CommonPlatforms.csproj Debug AnyCPU
@call :BuildCsproj Sharpmake.Application/Sharpmake.Application.csproj Debug AnyCPU

echo  cd /d %SHARPMAKEGEN_PATH%
cd /d %SHARPMAKEGEN_PATH%
echo @call %SHARPMAKE_EXECUTABLE% "/sources(\"SharpmakeGen.sharpmake.cs\")" /verbose
@call %SHARPMAKE_EXECUTABLE% /sources("SharpmakeGen.sharpmake.cs") /verbose
popd

echo Sharpmake solution generated.
goto end

@REM -----------------------------------------------------------------------
:BuildCsproj
echo Compiling %~1 in "%~2|%~3"...
msbuild /nologo /verbosity:minimal /p:Configuration="%~2" /p:Platform="%~3" "%~1"
if errorlevel 1 (
    echo ERROR: Failed to compile %~1 in "%~2|%~3".
    goto:error
)
exit /b 0

@REM -----------------------------------------------------------------------
:end
:: restore caller current directory
popd
@COLOR
exit /b 0

@REM -----------------------------------------------------------------------
:error
:: restore caller current directory
popd
@COLOR 4F
pause
exit /b 1
