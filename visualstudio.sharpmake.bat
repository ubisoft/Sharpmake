@echo off

@REM AUTO-ELEVATE the batch file to admin if needed!
net file 1>nul 2>nul && goto :run || powershell -ex unrestricted -Command "Start-Process -Wait -Verb RunAs -FilePath '%comspec%' -ArgumentList '/c \"%~fnx0\" %*'"
goto :eof
:run

for /f "delims=" %%A in ('"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -property instanceID') do set "VS_INSTANCEID=%%A"
set VS_PRIVATE_HIVE="%LOCALAPPDATA%\Microsoft\VisualStudio\15.0_%VS_INSTANCEID%\privateregistry.bin"
reg.exe load HKU\VS2017 %VS_PRIVATE_HIVE% 1>NUL 2>NUL
if ERRORLEVEL 1 (
    echo.
    echo             Please close all instances of Visual Studio 2017 before running this script.
    echo.
    goto err
)

set uniqueFileName=%tmp%\visualstudio.sharpmake~%RANDOM%.reg
powershell -Command "(gc %~d0%~p0visualstudio.sharpmake.reg) -replace 'VS_INSTANCEID', $Env:VS_INSTANCEID | Out-File %uniqueFileName%"

reg.exe import %uniqueFileName% 1>NUL 2>NUL

reg.exe unload HKU\VS2017 1>NUL 2>NUL
if ERRORLEVEL 1 (
    echo.
    echo     Failed to close %VS_PRIVATE_HIVE%
    echo.
    echo           You must use RegEdit and close it manually
    echo           ^(use File/Close Hive on HKEY_USERS\VS2017^)
    echo.
    echo           *********** You must do this otherwise VS2017 will not start *********** 
    echo.
    goto err
)

del /q %uniqueFileName%
rem exit

:err
pause
