@echo off

:: main
set ERRORLEVEL_BACKUP=0

call :UpdateRef samples ConfigureOrder    main.sharpmake.cs              reference ConfigureOrder
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CPPCLI            CLRTest.sharpmake.cs           reference CPPCLI
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpHelloWorld  HelloWorld.sharpmake.cs        reference CSharpHelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples HelloWorld        HelloWorld.sharpmake.cs        reference HelloWorld
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples CSharpVsix        CSharpVsix.sharpmake.cs        reference CSharpVsix
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples Fastbuild         Fastbuild.sharpmake.cs         reference Fastbuild
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
call :UpdateRef samples PackageReferences PackageReferences.sharpmake.cs reference PackageReferences
if not "%ERRORLEVEL_BACKUP%" == "0" goto error
:: that one is special, the root is the current folder
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

set SHARPMAKE_EXECUTABLE=%~2\bin\Debug\Sharpmake.Application.exe
if not exist %SHARPMAKE_EXECUTABLE% set SHARPMAKE_EXECUTABLE=%~2\bin\Release\Sharpmake.Application.exe
if not exist %SHARPMAKE_EXECUTABLE% echo Cannot find sharpmake executable in %~dp0%~1\%~2 & pause & goto error 

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
