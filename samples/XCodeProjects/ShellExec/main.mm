#include <cstdio>
#include <fmt/core.h>
#import "Globals.h"

bool ShellExec(const char* aFilename,
               const char* aDirectory /*= nullptr */,
               const char* aParameters /*= nullptr */,
               bool aWaitForCompletionFlag /*= true */,
               int* aExitCodeOut /*= nullptr */
)
{
#if IS_MAC_PLATFORM
    NSPipe *pipeStdOut = [NSPipe pipe];
    NSFileHandle *fileStdOut = pipeStdOut.fileHandleForReading;
    NSPipe *pipeStdErr = [NSPipe pipe];
    NSFileHandle *fileStdErr = pipeStdErr.fileHandleForReading;

    NSTask *task = [[NSTask alloc] init];
    task.executableURL = [NSURL fileURLWithPath: [NSString stringWithCString: aFilename encoding: NSUTF8StringEncoding] isDirectory: NO];
    if (aDirectory) task.currentDirectoryURL = [NSURL fileURLWithPath: [NSString stringWithCString: aDirectory encoding: NSUTF8StringEncoding] isDirectory: YES];
    if (aParameters)
    {
        NSString *params = [NSString stringWithCString: aParameters encoding: NSUTF8StringEncoding];
        NSArray *splitParams = [params componentsSeparatedByString:@" "];
        task.arguments = splitParams;
    }
    task.standardOutput = pipeStdOut;
    task.standardError = pipeStdErr;

    NSError* error = nil;
    BOOL stat = [task launchAndReturnError: &error];

    if (aWaitForCompletionFlag)
    {
        [task waitUntilExit];
        if (aExitCodeOut) *aExitCodeOut = task.terminationStatus;

        NSData *dataStdOut = [fileStdOut readDataToEndOfFile];
        [fileStdOut closeFile];
        NSData *dataStdErr = [fileStdErr readDataToEndOfFile];
        [fileStdErr closeFile];

        std::string stdOut = [[[NSString alloc] initWithData: dataStdOut encoding: NSUTF8StringEncoding] UTF8String];
        std::string stdErr = [[[NSString alloc] initWithData: dataStdErr encoding: NSUTF8StringEncoding] UTF8String];
        
        fmt::print("{}", stdOut);
        fmt::print("{}", stdErr);
    }

    return task.terminationStatus == 0;
#else
    // not supported by system, unless running on jailbroken device
    return false;
#endif // IS_MAC_PLATFORM
}

int main(int argc, char** argv)
{
    int exitCode = -1;
    if (ShellExec(
        "/bin/sh",
        nil,
        fmt::format("-c code --goto {}:{}", __FILE__, __LINE__).c_str(), //"-c echo Hello World",
        true,
        &exitCode
    ))
    {
        fmt::println("success");
    }
    fmt::println("======================");

    if (ShellExec(
        "/usr/local/bin/code",
        nil,
        fmt::format("--goto {}:{}", __FILE__, __LINE__).c_str(), //"-c echo Hello World",
        true,
        &exitCode
    ))
    {
        fmt::println("success");
    }
    fmt::println("======================");

    if (ShellExec(
        "/bin/sh",
        "/Applications",
        "-c ls -alG",
        true,
        &exitCode
    ))
    {
        fmt::println("success");
    }
    fmt::println("======================");
    
    if (ShellExec(
        "/bin/sh",
        nil,
        "-c ls -alG",
        true,
        &exitCode
    ))
    {
        fmt::println("success");
    }
    fmt::println("======================");

    if (ShellExec(
        "/bin/sh",
        "./",
        "-c ls -alG",
        true,
        &exitCode
    ))
    {
        fmt::println("success");
    }
    fmt::println("======================");

    if (ShellExec(
        "/bin/sh",
        "",
        "-c ls -alG",
        true,
        &exitCode
    ))
    {
        fmt::println("success");
    }
    fmt::println("======================");

    return exitCode;
}
