@echo off

SETLOCAL

call :DECOMPRESS_VCPKG_EXPORTED_PACKAGES
IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

call :GENERATE_PROJECTS
IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

:: Final Cleanup
exit /B %ERRORLEVEL%

:: ---------- FUNCTIONS ---------------

:: Decompress the exported vcpkg package for consumption by Sharpmake and generated projects
:DECOMPRESS_VCPKG_EXPORTED_PACKAGES
rmdir /Q /S extern\vcpkg >NUL 2>NUL
echo Extracting extern\vcpkg.zip to extern\vcpkg
powershell Expand-Archive extern\vcpkg.zip -DestinationPath extern
exit /B %ERRORLEVEL%

:: Generate projects using Sharpmake
:GENERATE_PROJECTS
%~dp0..\..\bin\Release\Sharpmake.Application.exe /sources(@".\sharpmake\main.sharpmake.cs") /generatedebugsolution
exit /B %ERRORLEVEL%





