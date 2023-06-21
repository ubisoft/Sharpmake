#include <cstdio>
#include <string>
#include <fmt/format.h>
#import "Globals.h"

int main(int argc, char** argv)
{
    bool anActivateVisualStudioFlag = false;
    const char* aFilename = __FILE__;
    int aLine = __LINE__;

    // VSCode --goto ./src/hello_world.c:123
    std::string codeCommand = fmt::format("/usr/local/bin/code {} --goto {}:{}",
            anActivateVisualStudioFlag ? "--new-window" : "--reuse-window",
            aFilename,
            aLine);
    int s1 = system(codeCommand.c_str());
    return s1;
}
