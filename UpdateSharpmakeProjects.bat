@echo off

:: main
set ERRORLEVEL_BACKUP=0

call :UpdateRef samples SharpmakeGen      SharpmakeGen.sharpmake.cs      reference %~dp0
if not "%ERRORLEVEL_BACKUP%" == "0" goto error

@COLOR 2F
echo References update succeeded!
timeout /t 5
goto end

:: function Update the reference folder that's used for regression tests
:: params:  testScopedCurrentDirectory,
::          folderPath,
::          mainFile,
::          outputDirectory
::          remapRootPath
:UpdateRef
:: backup current directory
pushd %CD%
:: set testScopedCurrentDirectory as current
cd /d %~dp0%~1

set SHARPMAKE_EXECUTABLE=%~dp0bin\Debug\Sharpmake.Application.exe
if not exist %SHARPMAKE_EXECUTABLE% set SHARPMAKE_EXECUTABLE=%~dp0bin\Release\Sharpmake.Application.exe
if not exist %SHARPMAKE_EXECUTABLE% echo Cannot find sharpmake executable in %~dp0bin & pause & goto error

echo Using executable %SHARPMAKE_EXECUTABLE%

call %SHARPMAKE_EXECUTABLE% "/sources(@"%~2\%~3") /verbose"
set ERRORLEVEL_BACKUP=%errorlevel%
:: restore caller current directory
popd
goto :end

:end
exit /b 0

:error
@COLOR 4F
pause
exit /b 1
