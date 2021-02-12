#!/usr/bin/env bash
# Script arguments:
# %1: Main sharpmake file
# $2: Target(Normally should be Debug or Release)
# if arguments are omitted, defaults to Sharpmake.Main.sharpmake.cs and Debug

function success {
	echo Bootstrap succeeded \!
	exit 0
}

function error {
	echo Bootstrap failed \!
	exit 1
}

# fail immediately if anything goes wrong
set -e

CURRENT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

which msbuild > /dev/null
MSBUILD_FOUND=$?
if [ $MSBUILD_FOUND -ne 0 ]; then
    echo "MSBuild not found"
    error
fi

# workaround for https://github.com/mono/mono/issues/6752
TERM=xterm

SHARPMAKE_OPTIM=${2:-"debug"}

SHARPMAKE_EXECUTABLE=$CURRENT_DIR/tmp/bin/$SHARPMAKE_OPTIM/Sharpmake.Application.exe

$CURRENT_DIR/CompileSharpmake.sh $CURRENT_DIR/Sharpmake.Application/Sharpmake.Application.csproj $SHARPMAKE_OPTIM AnyCPU
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
SM_CMD="mono --debug \"${SHARPMAKE_EXECUTABLE}\" \"/sources('${SHARPMAKE_MAIN}') /verbose\""
echo $SM_CMD
eval $SM_CMD || error

success
