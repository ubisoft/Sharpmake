#!/usr/bin/env bash
# Script arguments:
# $1: Project/Solution to build
# $2: Target(Normally should be Debug or Release)
# $3: Platform(Normally should be "Any CPU" for sln and AnyCPU for a csproj)
# $4: Framework (Optional, but can specified to only build for one, ex: net472 or net5.0)
# if none are passed, defaults to building Sharpmake.sln in Debug|Any CPU for all frameworks

function BuildSharpmake {
    solutionPath=$1
    configuration=$2
    platform=$3
    framework=$4

    BUILD_CMD="dotnet build -nologo --verbosity m -p:UseAppHost=true \"$solutionPath\" --configuration \"$configuration\""
    echoMessage="Compiling $solutionPath in ${configuration}|${platform}"
    if [ -n "$framework" ]; then
        BUILD_CMD="$BUILD_CMD --framework \"$framework\""
        echoMessage="$echoMessage|${framework}"
    fi

    echo $echoMessage
    echo $BUILD_CMD
    eval $BUILD_CMD
    if [ $? -ne 0 ]; then
        echo ERROR: Failed to compile $solutionPath in "${configuration}|${platform}|".
        exit 1
    fi
}

# fail immediately if anything goes wrong
set -e

CURRENT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

which dotnet > /dev/null
DOTNET_FOUND=$?
if [ $DOTNET_FOUND -ne 0 ]; then
    echo "dotnet not found, see https://dotnet.microsoft.com/download"
    exit $DOTNET_FOUND
fi

SOLUTION_PATH=${1:-"${CURRENT_DIR}/Sharpmake.sln"}
CONFIGURATION=${2:-"Debug"}
PLATFORM=${3:-"Any CPU"}
FRAMEWORK=$4

BuildSharpmake "$SOLUTION_PATH" "$CONFIGURATION" "$PLATFORM" "$FRAMEWORK"
