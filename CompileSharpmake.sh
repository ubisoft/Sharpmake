#!/usr/bin/env bash
# Script arguments:
# $1: Project/Solution to build
# $2: Target(Normally should be Debug or Release)
# $3: Platform(Normally should be "Any CPU" for sln and AnyCPU for a csproj)
# if none are passed, defaults to building Sharpmake.sln in Debug|Any CPU for all frameworks

function BuildSharpmake {
    solutionPath=$1
    configuration=$2
    platform=$3

    # Note: Disabling code signing on mac as otherwise we get such error:
    # error NETSDK1177: Failed to sign apphost with error code 0:
    # See https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/6.0/apphost-generated-for-macos
    # We can't disable UseAppHost as otherwise we won't have any executable. Disabling signing is not ideal but works for our case.
    MSBUILDLOGFILE="tmp/msbuild/${ImageOS}${platform}/msbuild_${configuration}.binlog"
    BUILD_CMD="dotnet build -nologo --verbosity m -bl:\"${MSBUILDLOGFILE}\" -p:UseAppHost=true /p:_EnableMacOSCodeSign=false \"$solutionPath\" --configuration \"$configuration\""
    echoMessage="Compiling $solutionPath in ${configuration}|${platform}"

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

BuildSharpmake "$SOLUTION_PATH" "$CONFIGURATION" "$PLATFORM"
