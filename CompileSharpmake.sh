#!/bin/bash

#Globals
platform=${1} # (Optional) Platform,  linux or ios. Default is linux. Always builds x64 version.

if [ "$platform" = "" ]; then
    platform=linux
fi

SCRIPT=$(readlink -f "$0")
SCRIPTPATH=$(dirname "$SCRIPT")
SHARPMAKE_EXECUTABLE="$SCRIPTPATH/bin/debug/netcoreapp2.0/$platform-x64/publish/Sharpmake.Application"
SHARPMAKE_EXECUTABLE_RELEASE="$SCRIPTPATH/bin/release/netcoreapp2.0/$platform-x64/publish/Sharpmake.Application"

# helpers to quiet pushd/popd
pushd () {
    command pushd "$@" > /dev/null
}

popd () {
    command popd "$@" > /dev/null
}

# Message helpers
EchoExecute() {
  echo $@
  $@
  return $?
}

SuccessMessage() {
    echo -e "\e[92m$@\e[0m"
}

SuccessWait() {
    SuccessMessage $@
    sleep 5
}

ErrorMessage() {
    echo -e "\e[31m$@\e[0m"
}

ErrorWait() {
  ErrorMessage $@
  read -p "Press [Enter] key to exit..."
}

# Compile function:
CompileSharpmake()
{
  file=${1} # Project/Solution to build
  target=${2} # Target(Normally should be Debug or Release)
  buildtype=${3} # Type of build to perforce, 'build' or 'publish'. Build will compile the solution to check for errors, but publish is needed to make an runnable executable

  # set batch file directory as current
  SCRIPT=$(readlink -f "$0")
  SCRIPTPATH=$(dirname "$SCRIPT")
  pushd "$SCRIPTPATH"

  # Build Sharpmake using specified arguments
  echo Compiling $file in "$target|$platform-x64"...

  BUILD_CMD="dotnet build \"$file\" -v q -nologo -c \"$target\""

  if [ "$buildtype" = "publish" ]; then
    BUILD_CMD="dotnet publish \"$file\" -v q -nologo -c \"$target\" -r $platform-x64 --self-contained true"
  fi

  echo $BUILD_CMD
  $BUILD_CMD

  errorlevel=$?

  # End of batch file
  popd

  if [ ! $errorlevel -eq 0 ]; then
      echo -e "\e[31mERROR: Failed to compile $file in \"$target|$platform-x64\".\e[0m"
      exit 1
  fi
}