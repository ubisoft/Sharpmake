@echo off

:: Clear previous run status
COLOR

:: First compile sharpmake to insure we are trying to deploy using an executable corresponding to the code.
dotnet build Sharpmake.sln /p:Configuration=Release /p:Platform="Any CPU"
if %errorlevel% NEQ 0 goto error

set SHARPMAKE_EXECUTABLE=%~dp0Sharpmake.Application\bin\Release\net6.0\Sharpmake.Application.exe
if not exist %SHARPMAKE_EXECUTABLE% echo Cannot find sharpmake executable in %~dp0Sharpmake.Application\bin\Release\net6.0 & pause & goto error

echo Using executable %SHARPMAKE_EXECUTABLE%

:: main
set ERRORLEVEL_BACKUP=0

:: samples
call :UpdateRef samples ConfigureOrder              main.sharpmake.cs                          reference         ConfigureOrder
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CPPCLI                      CLRTest.sharpmake.cs                       reference         CPPCLI
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpHelloWorld            HelloWorld.sharpmake.cs                    reference         CSharpHelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples JumboBuild                  JumboBuild.sharpmake.cs                    reference         JumboBuild
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples HelloWorld                  HelloWorld.sharpmake.cs                    reference         HelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples HelloLinux                  HelloLinux.Main.sharpmake.cs               reference         HelloLinux
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples HelloAssembly               HelloAssembly.sharpmake.cs                 reference         HelloAssembly
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpVsix                  CSharpVsix.sharpmake.cs                    reference         CSharpVsix
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpWCF                   CSharpWCF.sharpmake.cs                     reference         CSharpWCF
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpImports               CSharpImports.sharpmake.cs                 reference         CSharpImports
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples PackageReferences           PackageReferences.sharpmake.cs             reference         PackageReferences
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
:: skipped in regression tests
::call :UpdateRef samples QTFileCustomBuild           QTFileCustomBuild.sharpmake.cs             reference         QTFileCustomBuild
::if not "%ERRORLEVEL_BACKUP%" == "0" goto error
:: skipped in regression tests
::call :UpdateRef samples FastBuildSimpleExecutable   FastBuildSimpleExecutable.sharpmake.cs     reference         FastBuildSimpleExecutable\projects
::if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples SimpleExeLibDependency      SimpleExeLibDependency.sharpmake.cs        reference         SimpleExeLibDependency
if not "%ERRORLEVEL_BACKUP%" == "0" goto error

call :UpdateRef samples NetCore\DotNetCoreFrameworkHelloWorld    HelloWorld.sharpmake.cs       reference         NetCore\DotNetCoreFrameworkHelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples NetCore\DotNetFrameworkHelloWorld        HelloWorld.sharpmake.cs       reference         NetCore\DotNetFrameworkHelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples NetCore\DotNetMultiFrameworksHelloWorld  HelloWorld.sharpmake.cs       reference         NetCore\DotNetMultiFrameworksHelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples NetCore\DotNetOSMultiFrameworksHelloWorld  HelloWorld.sharpmake.cs     reference         NetCore\DotNetOSMultiFrameworksHelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error

:: functional tests
:: Skipped in regression tests
::call :UpdateRef Sharpmake.FunctionalTests FastBuildFunctionalTest FastBuildFunctionalTest.sharpmake.cs reference FastBuildFunctionalTest
::if not "%ERRORLEVEL_BACKUP%" == "0" goto error

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

echo Updating references of %2...
rd /s /q "%~2\%~4"
call %SHARPMAKE_EXECUTABLE% /sources(@'%~2\%~3') /outputdir(@'%~2\%~4') /remaproot(@'%~5') /verbose
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
