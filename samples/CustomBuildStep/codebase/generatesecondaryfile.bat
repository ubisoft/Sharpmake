@echo off
setlocal

set OUTPUT=%~dp0secondaryfile.cpp
echo Generating %OUTPUT%

:: Note: ^ is used to escape < and >
> "%OUTPUT%" echo #include ^<stdio.h^>
>>"%OUTPUT%" echo void PrintSecondaryFileContent()
>>"%OUTPUT%" echo {
>>"%OUTPUT%" echo     printf("This is secondary file\n");
>>"%OUTPUT%" echo }

echo Done
goto :eof