#!/bin/sh

# fail immediately if anything goes wrong
set -e

# workaround for https://github.com/mono/mono/issues/6752
TERM=xterm

# TODO: Test mono and msbuild existence
SHARPMAKE_EXECUTABLE=$PWD/bin/debug/Sharpmake.Application.exe

SM_CMD="msbuild -t:build -restore /p:Configuration=Debug /p:Platform="AnyCPU" /v:m Sharpmake.Application/Sharpmake.Application.csproj"
echo "Building Sharpmake..."
echo $SM_CMD
eval $SM_CMD

if [ $? -ne 0 ]; then
    echo "The build has failed."
    if [ -f $SHARPMAKE_EXECUTABLE ]; then
        echo "A previously built sharpmake exe was found at '$SHARPMAKE_EXECUTABLE', it will be reused."
    fi
fi

echo "Generating Sharpmake solution..."
mono --debug $SHARPMAKE_EXECUTABLE "/sources(\"Sharpmake.Main.sharpmake.cs\")" /verbose

