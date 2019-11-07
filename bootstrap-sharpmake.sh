#!/bin/bash
. ./CompileSharpmake.sh

# set batch file directory as current
pushd "$SCRIPTPATH"

CompileSharpmake Sharpmake.Application/Sharpmake.Application_Core.csproj Debug
errorlevel=$?
if [ $errorlevel -eq 0 ]; then
    EchoExecute $SHARPMAKE_EXECUTABLE "/sources('Sharpmake.Main.sharpmake.cs') /verbose"
    errorlevel=$?
    if [ $errorlevel -eq 0 ]; then
        SuccessWait "Bootstrap succeeded"
    fi
fi

if [ ! $errorlevel -eq 0 ]; then
    ErrorWait "Bootstrap failed"
    errorlevel=1
fi

# restore caller current directory
popd
exit ${errorlevel}
