#!/usr/bin/env bash
# Script arguments:
# %1: Main sharpmake file
# $2: Target(Normally should be Debug or Release)
# $3: Framework (net5.0 or any configured framework in Sharpmake.Main.sharpmake.cs)
# if arguments are omitted, defaults to Sharpmake.Main.sharpmake.cs in Debug with .net framework 5.0

function success {
    echo Bootstrap succeeded \!
    exit 0
}

function error {
    echo Bootstrap failed \!
    exit 1
}

CURRENT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

SHARPMAKE_MAIN="${1:-"Sharpmake.Main.sharpmake.cs"}"

which dotnet > /dev/null
DOTNET_FOUND=$?
if [ $DOTNET_FOUND -ne 0 ]; then
    echo "dotnet not found, see https://dotnet.microsoft.com/download"
    error
fi

SHARPMAKE_OPTIM=${2:-"debug"}
SHARPMAKE_FRAMEWORK=${3:-"net6.0"}

echo "Build and run sharpmake $SHARPMAKE_FRAMEWORK in $SHARPMAKE_OPTIM..."

# -p:UseAppHost=false

SM_CMD_RUN="dotnet run --verbosity m --project Sharpmake.Application/Sharpmake.Application.csproj --configuration $SHARPMAKE_OPTIM -- \"/sources('${SHARPMAKE_MAIN}') /verbose\""
echo $SM_CMD_RUN
eval $SM_CMD_RUN
if [ $? -ne 0 ]; then
    echo "DotNet run failed, trying to build first..."
    $CURRENT_DIR/CompileSharpmake.sh $CURRENT_DIR/Sharpmake.Application/Sharpmake.Application.csproj $SHARPMAKE_OPTIM AnyCPU
    if [ $? -ne 0 ]; then
        error
    fi

    SHARPMAKE_EXECUTABLE=$CURRENT_DIR/tmp/bin/$SHARPMAKE_OPTIM/$SHARPMAKE_FRAMEWORK/Sharpmake.Application.exe

    echo "Generating Sharpmake solution..."
    SM_CMD_RUN="\"${SHARPMAKE_EXECUTABLE}\" \"/sources('${SHARPMAKE_MAIN}') /verbose\""
    echo $SM_CMD_RUN
    eval $SM_CMD_RUN || error
fi

success
