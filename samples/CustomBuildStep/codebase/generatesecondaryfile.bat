@echo off
setlocal

if "%~2"=="" goto :usage

set OUTPUT=%~dp0\secondaryfile.cpp

echo Concatenating "%~f1" + "%~f2" into "%OUTPUT%"
>"%OUTPUT%" type "%~f1"
>>"%OUTPUT%" type "%~f2"

echo Done.
goto :eof

:usage
echo Usage: %~n0 file1.cpp file2.cpp
echo Concatenates file1.cpp and file2.cpp into %~dp0concatenated.cpp
exit /b 1
