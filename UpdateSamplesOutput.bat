@echo off

:: First compile sharpmake to insure we are trying to deploy using an executable corresponding to the code.
call CompileSharpmake.bat Sharpmake.sln Debug "Any CPU"
if %errorlevel% NEQ 0 goto error

:: main
set ERRORLEVEL_BACKUP=0

call :UpdateRef samples ConfigureOrder              main.sharpmake.cs                          reference         ConfigureOrder
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CPPCLI                      CLRTest.sharpmake.cs                       reference         CPPCLI
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpHelloWorld            HelloWorld.sharpmake.cs                    reference         CSharpHelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples HelloWorld                  HelloWorld.sharpmake.cs                    reference         HelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpVsix                  CSharpVsix.sharpmake.cs                    reference         CSharpVsix
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpWCF                   CSharpWCF.sharpmake.cs                     reference         CSharpWCF\codebase
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples PackageReferences           PackageReferences.sharpmake.cs             reference         PackageReferences
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples QTFileCustomBuild           QTFileCustomBuild.sharpmake.cs             reference         QTFileCustomBuild
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples FastBuildSimpleExecutable   FastBuildSimpleExecutable.sharpmake.cs     reference         FastBuildSimpleExecutable
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

echo Updating references of %2...
rd /s /q "%~2\%~4"
call %SHARPMAKE_EXECUTABLE% "/sources(@"%~2\%~3") /outputdir(@"%~2\%~4") /remaproot(@"%~5") /verbose"
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
