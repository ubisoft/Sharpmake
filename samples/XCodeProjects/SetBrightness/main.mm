#import "Globals.h"
#import "BrightnessControl.h"

int main(int argc, char** argv)
{
    if (argc > 1)
    {
        SetScreenBrightness(atof(argv[1]));
    }
    else
    {
        SetScreenBrightness(0.5f);
    }

    return 0;
}
