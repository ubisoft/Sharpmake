@echo off
:: This batch file is used to update the vcpkg packages that we need for this sample. The packages
:: are then exported in a zip file ready for consumption on build machine or programmer pc.

SETLOCAL
SET VCPKG_LIST=curl:x64-windows-static rapidjson:x64-windows-static
SET VCPKG_TMPFOLDER=%~dp0\tmp\vcpkg

CALL :GET_VCPKG
IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

CALL :BUILD_VCPKG
IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

CALL :INSTALL_VCPKG_PACKAGES
IF ERRORLEVEL 1 exit /b %ERRORLEVEL%

CALL :GENERATE_VCPKG_EXPORT
IF ERRORLEVEL 1 exit /b %ERRORLEVEL%


:: Cleanup
EXIT /B %ERRORLEVEL%

:: ------------ FUNCTIONS ----------

:: clone vcpkg in a temporary folder
:GET_VCPKG
IF NOT EXIST %VCPKG_TMPFOLDER% GOTO CLONE_VCPKG
GOTO PULL_VCPKG

:CLONE_VCPKG
echo clone vcpkg
git clone https://github.com/microsoft/vcpkg.git %VCPKG_TMPFOLDER%
exit /B %ERRORLEVEL%

:PULL_VCPKG
echo pull latest vcpkg
pushd  %VCPKG_TMPFOLDER%
git pull
popd
exit /B %ERRORLEVEL%

:: Bootstrap vcpkg executable
:BUILD_VCPKG
IF EXIST %VCPKG_TMPFOLDER%\vcpkg.exe exit /B 0
call %VCPKG_TMPFOLDER%\bootstrap-vcpkg.bat
exit /B %ERRORLEVEL%

:: Install required vcpkg packages
:INSTALL_VCPKG_PACKAGES
%VCPKG_TMPFOLDER%\vcpkg.exe install %VCPKG_LIST%
exit /B %ERRORLEVEL%

:: Generate export package. This is what we submit in git
:: This package then need to be decompressed to be used. Will be decompresssed in extern/vcpkg
:GENERATE_VCPKG_EXPORT
%VCPKG_TMPFOLDER%\vcpkg.exe export %VCPKG_LIST% --zip --output=vcpkg --output-dir=%~dp0\extern
exit /B %ERRORLEVEL%
