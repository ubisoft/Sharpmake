#!/usr/bin/env bash
# Script arguments:
# $1: Project/Solution to build
# $2: Target(Normally should be Debug or Release)
# $3: Platform(Normally should be "Any CPU" for sln and AnyCPU for a csproj)
# if none are passed, defaults to building Sharpmake.sln in Debug|Any CPU

function BuildSharpmake {
    solutionPath=$1
    configuration=$2
    platform=$3
    echo Compiling $solutionPath in "${configuration}|${platform}"...
    MSBUILD_CMD="msbuild -t:build -restore \"${solutionPath}\" /nologo /v:m /p:Configuration=${configuration} /p:Platform=\"${platform}\""
    echo $MSBUILD_CMD
    eval $MSBUILD_CMD
    if [ $? -ne 0 ]; then
        echo ERROR: Failed to compile $solutionPath in "${configuration}|${platform}".
        exit 1
    fi
}

# fail immediately if anything goes wrong
set -e

CURRENT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

which msbuild > /dev/null
MSBUILD_FOUND=$?
if [ $MSBUILD_FOUND -ne 0 ]; then
    echo "MSBuild not found"
    exit $MSBUILD_FOUND
fi

SOLUTION_PATH=${1:-"${CURRENT_DIR}/Sharpmake.sln"}
CONFIGURATION=${2:-"Debug"}
PLATFORM=${3:-"Any CPU"}

BuildSharpmake "$SOLUTION_PATH" "$CONFIGURATION" "$PLATFORM"
