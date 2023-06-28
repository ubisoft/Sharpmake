# Initial stage perform makefile generation with Sharpmake using .NET 6 runtime image.
# This environment contains only .NET runtime necessary to execute Sharpmake.
FROM  mcr.microsoft.com/dotnet/runtime:6.0 AS sharpmake-env

ARG FRAMEWORK=net6.0

# Copy binaries and sample to container.
# Sharpmake binaries are assumed to exists prior to build the container.
COPY Sharpmake.Application/bin/Release/$FRAMEWORK Sharpmake
COPY samples/HelloLinux samples/HelloLinux

# Execute HelloLinux sample.
RUN dotnet Sharpmake/Sharpmake.Application.dll "/sources('samples/HelloLinux/HelloLinux.Main.sharpmake.cs')" /verbose

# Second stage perform C++ compilation of the sample using GCC image.
# This environment has no .NET runtime. But it contains the C++ libraries and tools needed to compile the sample.
# Altough its not the latest GCC image version, its the highest that produce a compatible binary that can be run
# with the libstdc++ used by the PowerShell image of the last stage.
FROM gcc:10.4.0-bullseye AS compiler-env

ARG CONFIGURATION=release

# Copy sample to container from previous stage.
# This include generated makefile.
COPY --from=sharpmake-env samples/HelloLinux samples/HelloLinux

# Sample C++ compilation.
WORKDIR /samples/HelloLinux/codebase/temp/solutions
RUN make -f HelloLinux_linux_make.make config=$CONFIGURATION

# Last stage is the container app that will executed.
# Its based on PowerShell image and contains binaries and scripts required to execute HelloLinux sample.
FROM debian:bullseye

COPY --from=compiler-env samples/HelloLinux samples/HelloLinux

ARG CONFIGURATION=release

# Running the resulting container app execute the sample.
ENV RUN_SAMPLE_CMD="./samples/HelloLinux/codebase/temp/bin/linux_$CONFIGURATION/exe"
CMD $RUN_SAMPLE_CMD
