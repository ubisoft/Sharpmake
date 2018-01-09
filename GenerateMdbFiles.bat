:: Script that generates Mono MDB files from Windows/.NET PDB files.

@echo off
setlocal enabledelayedexpansion

set CONFIGURATIONS=
if "%~1" equ "" (
    set CONFIGURATIONS=Debug Release
) else if /i "%~1" equ "debug" (
    set CONFIGURATIONS=Debug
) else if /i "%~1" equ "release" (
    set CONFIGURATIONS=Release
) else (
    echo.
    echo [%~nx0] ERROR: Unknown configuration type: %1^^!
    exit /b 1
)

pushd "%~dp0"

:: pdb2mdb.exe was originally taken from https://gist.github.com/jbevain/ba23149da8369e4a966f
:: as that version:
::  1) Handles the VS2015+ PDB format, and
::  2) Can be launched using .NET (i.e. does not need to run within Mono)
:: We now use another executable (still compliant with the said criteria), fetched with NuGet:
nuget install Mono.Unofficial.pdb2mdb

:: Look for pdb2mdb.exe. Its root directory has an undetermined name, because it contains a version
:: e.g. Mono.Unofficial.pdb2mdb.4.2.3.4.
set PDB2MDB_EXE=
for /f %%A in ('dir /b /s pdb2mdb.exe') do (
    set PDB2MDB_EXE=%%A
)

if not defined PDB2MDB_EXE (
    echo.
    echo [%~nx0] ERROR: Could not find pdb2mdb.exe^^!
    exit /b 1
)

:: For each specified configuration
for %%c in (%CONFIGURATIONS%) do (
    :: For each subdirectory...
    for /R "." /D %%d in (*) do (
        :: ...that matches the current configuration
        if /i "%%~nxd" equ "%%c" (
            :: For each EXE and DLL file
            for %%f in (%%d\*.exe %%d\*.dll) do (
                call :PROCESS_BIN_FILE %%f
            )
        )
    )
)

exit /b 0


:PROCESS_BIN_FILE
:: If this binary file does not have an associated PDB file, skip it!
if not exist %~dpn1.pdb (
    goto :EOF
)
set FILENAME=%~1
echo *** Generating MDB file for "!FILENAME:%CD%\=!"... ***

%PDB2MDB_EXE% %1

if not exist "%~1.mdb" (
    echo ERROR: "%~1.mdb" was NOT generated^^!
)
