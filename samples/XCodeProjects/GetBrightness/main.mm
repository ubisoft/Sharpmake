#import "Globals.h"
#import "BrightnessControl.h"

#import <fmt/core.h>
#import <fmt/format.h>

int main(int argc, char** argv)
{
    fmt::println("brightness: {}", GetScreenBrightness());
    return 0;
}
