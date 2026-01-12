@echo off
SET FILENAME=%~dp0\main.cpp

echo int main(int, char**) > %FILENAME%
echo { >> %FILENAME%
echo    extern void PrintConcatenatedFileContents(); >> %FILENAME%
echo    PrintConcatenatedFileContents(); >> %FILENAME%
echo 	return 0; >> %FILENAME%
echo } >> %FILENAME%
