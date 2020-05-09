#!/bin/sh

function pause {
 	read -s -n 1 -p "Press any key to continue . . ."
 	echo ""
}

function success {
	echo Bootstrap succeeded \!
	exit 0
}

function error {
	echo Bootstrap failed \!
	pause
	exit 1
}

# fail immediately if anything goes wrong
set -e

pushd $(dirname $0) > /dev/null
CURRENT_DIR=$(pwd)
popd > /dev/null

which msbuild > /dev/null
MSBUILD_FOUND=$?
if [ $MSBUILD_FOUND -ne 0 ]; then
    echo "MSBuild not found"
    error
fi

# workaround for https://github.com/mono/mono/issues/6752
TERM=xterm

# TODO: Test mono and msbuild existence
SHARPMAKE_EXECUTABLE=$CURRENT_DIR/bin/debug/Sharpmake.Application.exe

sh $CURRENT_DIR/CompileSharpmake.sh Sharpmake.Application/Sharpmake.Application.csproj Debug AnyCPU
if [ $? -ne 0 ]; then
    echo "The build has failed."
    if [ -f $SHARPMAKE_EXECUTABLE ]; then
        echo "A previously built sharpmake exe was found at '${SHARPMAKE_EXECUTABLE}', it will be reused."
    fi
fi

which mono > /dev/null
MONO_FOUND=$?
if [ $MONO_FOUND -ne 0 ]; then
    echo "Mono not found"
    error
fi

SHARPMAKE_MAIN=${1:-"$CURRENT_DIR/Sharpmake.Main.sharpmake.cs"}

echo "Generating Sharpmake solution..."
SM_CMD="mono --debug \"${SHARPMAKE_EXECUTABLE}\" \"/sources(\\\"${SHARPMAKE_MAIN}\\\")\" /verbose"
echo $SM_CMD
eval $SM_CMD || error

success