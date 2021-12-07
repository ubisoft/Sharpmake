@echo off

setlocal

@rem Workaround for fastbuild removing all environment variables for Exec calls.
SET PATH=%PATH%;C:\Windows\System32
set "keySystem=hklm\system\currentcontrolset\control\session manager\environment"
call :GETREGVAL "%keySystem%" path systemPath
SET PATH=%systemPath%

REM ********************************************************************************
REM BEGIN
REM ********************************************************************************

REM Execute what was passed by arguments
%*

REM ********************************************************************************
REM END
REM ********************************************************************************

goto :END

:ERROR
exit /b 1

:END
exit /b 0

REM ********************************************************************************
REM
REM GETREGVAL
REM Description: will return the value of a registry key value. Will substitute
REM              a "?" for any "%" so that values are not automatically expanded.
REM Parameters: key - the key string  to find in the registry
REM 		value - the key value to return
REM		return - the name of environment variable to return the value.
REM
REM ********************************************************************************
:GETREGVAL
:: The key to query
set "key=%~1"
:: The value to find
set "value=%2"
:: The environment variable for return value
set "return=%3"

:: No. of words in value 
Set count=1
For  %%j in ("%value%") Do Set /A count+=1
setlocal enabledelayedexpansion
Set "regVal="
REM FOR /F "tokens=%count%*" %%A IN ('REG.EXE QUERY "%key%" /V %value% 2^> nul') DO (SET "regVal=%%B")
REM cannot pipe error to nul if we wish to detect error level
REM For loop seems to bury the error level must pipe error to file
:: Need to access reg twice - unable to access errorlevel in for loop?
:: No longer concerned with registry error
:: If registry corrupted ant path not discoverable - many other problems would exist
::REG.EXE QUERY "%key%" /V %value% > nul
::if errorlevel 1 exit /b 1
FOR /F "tokens=%count%*" %%A IN ('REG.EXE QUERY "%key%" /V %value% 2^> nul') DO ( SET "regVal=%%B")

:: Was going to check for valid entries in path
:: however, if not valid that is for user to decide.
:: Provide mechanism in clean to eliminate invalid entries.
:: substitute ? for % so that values not automatically expanded
set "regval=!regVal:%%=?!"
:: Remove quotes 
set regval=!regVal:"=!
if "%regval%" equ "%%=?" (set "regVal=")

endlocal & Set "%return%=%regVal%"

exit /b 0