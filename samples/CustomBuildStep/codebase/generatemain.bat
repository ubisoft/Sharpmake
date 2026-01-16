@echo off
setlocal

set OUTPUT=%~dp0main.cpp
echo Generating %OUTPUT%

> "%OUTPUT%" echo #include ^<stdio.h^>
>>"%OUTPUT%" echo extern void PrintSecondaryFileContent();
>>"%OUTPUT%" echo int main(int, char**)
>>"%OUTPUT%" echo {
>>"%OUTPUT%" echo     printf("CustomBuildStep test\n");
>>"%OUTPUT%" echo     PrintSecondaryFileContent();
>>"%OUTPUT%" echo     return 0;
>>"%OUTPUT%" echo }

echo Done.
goto :eof
