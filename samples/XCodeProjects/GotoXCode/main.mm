#include <cstdio>
#include <string>
#include <fmt/format.h>
#import "Globals.h"

int main(int argc, char** argv)
{
    bool anActivateVisualStudioFlag = false;
    const char* aFilename = __FILE__;
    int aLine = __LINE__;

    // XCode xed -b -l 123 ./src/hello_world.c
    std::string xedCommand = fmt::format("xed {} -l {} {}",
        anActivateVisualStudioFlag ? "-x" : "",
        aLine,
        aFilename);
    int s0 = system(xedCommand.c_str());
    return s0;
}
