#import "Globals.h"
#import <fmt/core.h>
#import <sys/sysctl.h>


bool IsDebuggerAttached()
{
    // adapted from HockyApp-IOS
    // https://github.com/bitstadium/HockeySDK-iOS/blob/5abb855205d5bef4fa250833056a96f6ae5cb9e6/Classes/BITHockeyHelper.m#L346

    static bool debuggerIsAttached = false;

    static dispatch_once_t debuggerPredicate;
    dispatch_once(&debuggerPredicate, ^{
        struct kinfo_proc info;
        size_t info_size = sizeof(info);
        int name[4] = {
            CTL_KERN,
            KERN_PROC,
            KERN_PROC_PID,
            getpid(),
        };

        if (sysctl(name, 4, &info, &info_size, NULL, 0) == -1)
        {
            debuggerIsAttached = false;
        }
        
        if (!debuggerIsAttached && (info.kp_proc.p_flag & P_TRACED) != 0)
        {
            debuggerIsAttached = true;
        }
    });

    return debuggerIsAttached;
}

int main(int argc, char** argv)
{
    if (IsDebuggerAttached())
    {
        fmt::println("Hello Debugger");
    }
    else
    {
        fmt::println("Hello Freedom");
    }

    return 0;
}
