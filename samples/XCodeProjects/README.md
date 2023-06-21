# XCodeProjects

This folder contains a number of XCode projects targeted to run on MacOS, respectively iOS or tvOS.
The main purpose of these projects is

1. to illustrate the correct setup of the sharpmake scripts
2. to provide a use-case for XCode-specific features (such as embedding files into an Application Bundle)
3. to showcase a bit of MacOS or IOS functionality

## Project details

* `extern/StoreKit` showcases how to conditionally inject a dependency on a system framework
* `extern/fmt` showcases how to setup a sharpmake script for a 3rd party library, in this case the well known[{fmt}](https://fmt.dev/)
* `CLITool` is a simple 'hello world' to illustrate how to compile a command line tool
* `ToPasteboard` and `FromPasteboard` illustrate how to access the system clipboard
* `BrightnessControl`, `GetBrightness` and `SetBrightness` illustrate how to access a private framework to get/set the screen brightness.
* `HasDebugger` shows how to check whether the program is running in the debugger
* `GotoVSCode` and `GotoXCode` illustrate how to open a file at the given line in either IDE
* `ShowInFinder` illustrates how to open Finder over a given file
* `OpenAppStore` illustrates how to open the AppStore for a given application
* `OpenSettings` illustrates how to open system settings from an application
* `SysInfo` gathers system information through internal API
* `ShellExec` showcases the MacOS/Objective-C method to run a shell command from within another program
* `ReadAppData` showcases how to embed resource files inside an application bundle and how to read the embedded data
* `SampleBundle` illustrates how to set up a sharpmake script to create a 'Bundle' project
* `MetalNoStoryboard` illustrates how to create a simple Metal application without using a storyboard
* `MetalWithStoryboard` illustrates how to create a simple Metal application with a storyboard, as there are some specific settings to perform in the sharpmake script
* `HelloKitFramework` illustrates how to create a custom framework bundle
* `HelloKitConsumer` illustrates how to link against a custom framework bundle
