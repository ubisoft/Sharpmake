# Platforms

## Open Platforms

Sharpmake ships with the platform extension *Sharpmake.CommonPlatforms.dll*. This is a .NET class library that contains everything you need to generate Visual Studio solutions for C++, C#, and C++/CLI code for Windows. Sharpmake also has support for generating Xcode and GNU Make-based projects for Mac and Linux.

## NDA Platforms

Sharpmake was originally designed with built-in support for generating projects and solutions for video game consoles, which are platforms under strict NDA and private SDKs.

In order to release Sharpmake to the open-source community, these platforms have been removed from the code base and an extension mechanism have been created to allow developers to add them back at runtime.

If you are an *authorized developer* outside of Ubisoft and would like to try Sharpmake to generate projects for Microsoft, Nintendo or Sony platforms, please contact one of the Sharpmake maintainers through the first party developer forums.

## Referencing platforms from scripts

Platforms are compiled as ordinary .NET class libraries that are referenced into Sharpmake script. Because scripts are standalone though, there is no `.csproj` to add that reference to. Instead, you specify a platform reference with the `Sharpmake.Reference` module attribute:

```csharp
    [module: Reference("<path-of-your-platform>.dll")]
```

Sharpmake will look for DLL files in the executable's directory, although a relative path from the executable should work.

Please note that *Sharpmake.CommonPlatforms.dll* is always referenced, so putting `[module: Reference("Sharpmake.CommonPlatforms.dll")]` in your scripts is redundant.

There is no problem referencing platforms directly from scripts. The platform system is designed to isolate Sharpmake itself from the platforms, not the scripts.
